using RudeBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RudeBot.Services
{
    public interface IChatSettingsService
    {
        Task LoadAllChatSettings();
        Task<ChatSettings> AddOrUpdateChatSettings(ChatSettings settings);
        Task<ChatSettings> GetChatSettings(long chatId);
    }
}
