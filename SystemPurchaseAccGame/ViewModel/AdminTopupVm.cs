using System.ComponentModel.DataAnnotations;

namespace SystemPurchaseAccGame.ViewModel
{
    public class AdminTopupVm
    {
        public long TopupId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";

        public string Method { get; set; } = "";     // "Card" / "BANK"
        public long Amount { get; set; }
        public string Status { get; set; } = "";     // "PENDING" / "APPROVED" / "REJECTED"
        public string? ReferenceCode { get; set; }
        public string? RawPayload { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Provider { get; set; }  // card provider

    }
    public class CategoryRowVm
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int GameCount { get; set; } // số game thuộc category
    }
    public class GameRowVm
    {
        public int GameId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";

        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ListingCount { get; set; } // AccountListings count
    }
    public class CheapGameRowVm
    {
        public int GameId { get; set; }
        public int CategoryId { get; set; } // luôn = 6
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public bool IsActive { get; set; }
        public decimal? Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class CheapGameCreateDto
    {
        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        public string? Slug { get; set; }

        public string? ThumbnailUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal? Price { get; set; } // game price
    }

    public class CheapGameUpdateDto
    {
        [Required]
        public int GameId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = "";

        public string? Slug { get; set; }

        public string? ThumbnailUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal? Price { get; set; }
    }

    public class CheapGameDeleteDto
    {
        [Required]
        public int GameId { get; set; }
    }
}
