using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StickerKeeperBot.Models
{
    [Table("stickers")]
    public class Sticker : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("file_id")]
        public string FileId { get; set; }
        [Column("category")]
        public string Category { get; set; }
    }
}
