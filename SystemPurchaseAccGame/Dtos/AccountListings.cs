using System.ComponentModel.DataAnnotations;

namespace SystemPurchaseAccGame.Dtos
{
    public class AccountListingCreateRequest
    {
        // ===== Listing (AccountListings) =====
        [Required]
        public int GameId { get; set; }

        [Required, StringLength(120, MinimumLength = 5)]
        public string Title { get; set; } = default!;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = "DRAFT"; // DRAFT/PENDING/PUBLISHED/SOLD/HIDDEN...

        // ===== Attributes (AccountAttributes) =====
        // Client sẽ gửi JSON string => server parse ra list
        public string? AttributesJson { get; set; }

        // ===== LoginInfo (đang nằm trong AccountListings.LoginInfo) =====
        public LoginInfoDto? LoginInfo { get; set; }

        // ===== Media (AccountMedia) =====
        public IFormFile? BackgroundFile { get; set; }   // MediaType=background
        public IFormFile? ThumbnailFile { get; set; }    // MediaType=thumbnail
        public List<IFormFile>? GalleryFiles { get; set; } // MediaType=gallery

        // Publish action (optional)
        public string? Action { get; set; } // save/submitPending/publishNow...
    }

    public class LoginInfoDto
    {
        public string? User { get; set; }
        public string? Pass { get; set; }
        public string? Note { get; set; }
        public bool MaskPublic { get; set; } = true;
    }

    public class AttributeItemDto
    {
        public string Key { get; set; } = default!;
        public string? Value { get; set; }
    }
}
