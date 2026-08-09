using InventoryX.Domain.Models.Common;

namespace InventoryX.Domain.Models.Selling
{
    public class FavouritesLayout : BaseModel
    {
        public Guid RegisterId { get; set; }
        public Register? Register { get; set; }
        public string LayoutJson { get; set; } = "{}";
    }
}
