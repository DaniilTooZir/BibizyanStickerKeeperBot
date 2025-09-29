using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;

namespace StickerKeeperBot.Data
{
    public class StickerDb
    {
        public Client Client { get; private set; }
        public StickerDb(string url, string key)
        {
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false
            };
            Client = new Client(url, key, options);
        }
        public async Task InitAsync()
        {
            await Client.InitializeAsync();
        }
    }
}
