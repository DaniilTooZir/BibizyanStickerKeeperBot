using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StickerKeeperBot.Models;
using StickerKeeperBot.Data;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace StickerKeeperBot.Services
{
    public class StickerService
    {
        private readonly StickerDb _db;
        public StickerService(StickerDb db)
        {
            _db = db;
        }
        public async Task AddSticker(string name, string fileId)
        {
            var sticker = new Sticker
            {
                Name = name,
                FileId = fileId
            };
            await _db.Client.From<Sticker>().Insert(sticker);
        }
        public async Task<List<Sticker>> SearchStickers(string query)
        {
            var response = await _db.Client
                .From<Sticker>()
                .Filter("name", Operator.ILike, $"%{query}%")
                .Get();
            return response.Models;
        }
    }
}
