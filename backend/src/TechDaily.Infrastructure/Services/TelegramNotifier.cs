using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TechDaily.Application.Interfaces;

namespace TechDaily.Infrastructure.Services;

public class TelegramNotifier : ITelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramNotifier> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _botToken = configuration["Telegram:BotToken"] ?? string.Empty;
    }

    public async Task<bool> SendDailyDispatchAsync(
        long chatId,
        string topicTitle,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            _logger.LogInformation("[Mock Telegram] 08:00 AM Dispatch to ChatId {ChatId}: Today's Topic '{TopicTitle}'", chatId, topicTitle);
            return true;
        }

        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        var text = isVi
            ? $"🌅 *TechDaily — Thử thách hôm nay*\n\n📚 *Chủ đề:* {topicTitle}\n\n👉 [Bấm vào đây để học ngay](http://localhost:3000/today)"
            : $"🌅 *TechDaily — Daily Focus*\n\n📚 *Topic:* {topicTitle}\n\n👉 [Start Daily Drill](http://localhost:3000/today)";

        return await SendTelegramMessageAsync(chatId, text, cancellationToken);
    }

    public async Task<bool> SendStreakWarningAsync(
        long chatId,
        int currentStreak,
        string locale = "en",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken))
        {
            _logger.LogInformation("[Mock Telegram] 20:00 PM Streak Warning to ChatId {ChatId}: Streak {Streak}", chatId, currentStreak);
            return true;
        }

        var isVi = locale.Equals("vi", StringComparison.OrdinalIgnoreCase);
        var text = isVi
            ? $"🔥 *Cảnh báo Streak TechDaily*\n\nBạn đang có chuỗi *{currentStreak} ngày* liên tiếp! Đừng để mất chuỗi hôm nay.\n\n👉 [Hoàn thành bài học trước 24:00](http://localhost:3000/today)"
            : $"🔥 *TechDaily Streak Warning*\n\nYou currently have a *{currentStreak}-day streak*! Complete today's drill before midnight to keep your momentum.\n\n👉 [Complete Drill Now](http://localhost:3000/today)";

        return await SendTelegramMessageAsync(chatId, text, cancellationToken);
    }

    private async Task<bool> SendTelegramMessageAsync(long chatId, string markdownText, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = markdownText,
                parse_mode = "Markdown"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message to chatId {ChatId}", chatId);
            return false;
        }
    }
}
