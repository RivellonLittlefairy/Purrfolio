using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Purrfolio.App.Models;

namespace Purrfolio.App.Services;

public sealed class MinimaxOcrService(HttpClient httpClient) : IMinimaxOcrService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<OcrInvestmentCandidate>> AnalyzeScreenshotAsync(
        string imagePath,
        MinimaxRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("截图文件不存在。", imagePath);
        }

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException("MiniMax Endpoint 未设置。请填写接口地址。\n示例：https://api.minimax.chat/v1/text/chatcompletion_v2");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("MiniMax API Key 未设置。请填写 API Key。\n可通过环境变量 MINIMAX_API_KEY 预置。\n");
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var mimeType = ResolveMimeType(imagePath);
        var base64 = Convert.ToBase64String(imageBytes);

        var prompt = BuildPrompt();

        var payload = new
        {
            model = options.Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a financial OCR extractor. Return only valid JSON."
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = prompt
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{mimeType};base64,{base64}"
                            }
                        }
                    }
                }
            },
            response_format = new
            {
                type = "json_object"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (options.UseBearerToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }
        else
        {
            request.Headers.TryAddWithoutValidation("Authorization", options.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"MiniMax 请求失败：{(int)response.StatusCode} {response.ReasonPhrase}\n{raw}");
        }

        var jsonText = StripMarkdownJsonFence(TryExtractJsonText(raw));
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("MiniMax 返回结果中未找到可解析 JSON。请检查接口兼容模式。\n\n原始返回：\n" + raw);
        }

        var result = ParseCandidates(jsonText);
        if (result.Count == 0)
        {
            throw new InvalidOperationException("识别成功但未提取到可导入记录。请换更清晰截图或手动编辑。");
        }

        return result;
    }

    private static IReadOnlyList<OcrInvestmentCandidate> ParseCandidates(string jsonText)
    {
        using var doc = JsonDocument.Parse(jsonText);

        var root = doc.RootElement;

        JsonElement recordsElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            recordsElement = root;
        }
        else if (root.TryGetProperty("records", out var records))
        {
            recordsElement = records;
        }
        else if (root.TryGetProperty("items", out var items))
        {
            recordsElement = items;
        }
        else
        {
            return [];
        }

        if (recordsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<OcrInvestmentCandidate>();

        foreach (var node in recordsElement.EnumerateArray())
        {
            var candidate = new OcrInvestmentCandidate(
                Name: GetString(node, "name", "资产") ?? "未命名资产",
                AssetClass: GetString(node, "assetClass", "asset_class") ?? "GovernmentBonds",
                TradeDate: GetString(node, "tradeDate", "trade_date") ?? DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
                Quantity: GetDecimal(node, "quantity") ?? 0m,
                UnitPrice: GetDecimal(node, "unitPrice", "unit_price", "price") ?? 0m,
                Fees: GetDecimal(node, "fees", "fee") ?? 0m,
                CouponRate: GetDecimal(node, "couponRate", "coupon_rate") ?? 0m,
                CouponFrequency: GetString(node, "couponFrequency", "coupon_frequency") ?? "SemiAnnual",
                MaturityDate: GetString(node, "maturityDate", "maturity_date"),
                AccruedInterest: GetDecimal(node, "accruedInterest", "accrued_interest") ?? 0m,
                IsSpecialGovernmentBond: GetBoolean(node, "isSpecialGovernmentBond", "is_special_government_bond") ?? false,
                Confidence: GetDouble(node, "confidence", "score") ?? 0.5d);

            list.Add(candidate);
        }

        return list;
    }

    private static string? TryExtractJsonText(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices)
                    && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var msg)
                        && msg.TryGetProperty("content", out var content))
                    {
                        if (content.ValueKind == JsonValueKind.String)
                        {
                            return content.GetString();
                        }

                        if (content.ValueKind == JsonValueKind.Array)
                        {
                            var sb = new StringBuilder();
                            foreach (var part in content.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
                                {
                                    sb.Append(textNode.GetString());
                                }
                            }

                            var text = sb.ToString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                return text;
                            }
                        }
                    }
                }

                if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
                {
                    return outputText.GetString();
                }

                if (root.TryGetProperty("records", out _) || root.ValueKind == JsonValueKind.Array)
                {
                    return raw;
                }
            }
            catch
            {
                // ignored
            }
        }

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return raw[start..(end + 1)];
        }

        return raw;
    }

    private static string? StripMarkdownJsonFence(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        var trimmed = input.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            return trimmed;
        }

        var firstLineBreak = trimmed.IndexOf('\n');
        if (firstLineBreak < 0)
        {
            return trimmed.Trim('`');
        }

        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence <= firstLineBreak)
        {
            return trimmed;
        }

        return trimmed.Substring(firstLineBreak + 1, lastFence - firstLineBreak - 1).Trim();
    }

    private static string BuildPrompt()
    {
        return
            "从图片中提取投资记录并只返回 JSON。" +
            "JSON schema: {\"records\":[{\"name\":\"\",\"assetClass\":\"Stocks|Gold|GovernmentBonds|Cash\",\"tradeDate\":\"yyyy-MM-dd\",\"quantity\":0,\"unitPrice\":0,\"fees\":0,\"couponRate\":0,\"couponFrequency\":\"Annual|SemiAnnual|Quarterly|Monthly\",\"maturityDate\":\"yyyy-MM-dd\",\"accruedInterest\":0,\"isSpecialGovernmentBond\":false,\"confidence\":0.0}]}" +
            "。若某字段不确定也要填默认值并给低 confidence。";
    }

    private static string ResolveMimeType(string imagePath)
    {
        return Path.GetExtension(imagePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/png"
        };
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.String)
            {
                return node.GetString();
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var node))
            {
                continue;
            }

            if (node.ValueKind == JsonValueKind.Number && node.TryGetDecimal(out var number))
            {
                return number;
            }

            if (node.ValueKind == JsonValueKind.String)
            {
                var text = node.GetString();
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
                {
                    return invariant;
                }

                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var localized))
                {
                    return localized;
                }
            }
        }

        return null;
    }

    private static double? GetDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var node))
            {
                continue;
            }

            if (node.ValueKind == JsonValueKind.Number && node.TryGetDouble(out var number))
            {
                return number;
            }

            if (node.ValueKind == JsonValueKind.String)
            {
                var text = node.GetString();
                if (double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant))
                {
                    return invariant;
                }

                if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var localized))
                {
                    return localized;
                }
            }
        }

        return null;
    }

    private static bool? GetBoolean(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var node))
            {
                continue;
            }

            if (node.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (node.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (node.ValueKind == JsonValueKind.String && bool.TryParse(node.GetString(), out var textBool))
            {
                return textBool;
            }
        }

        return null;
    }
}
