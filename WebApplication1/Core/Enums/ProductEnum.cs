namespace WebApplication1.Enums
{
    public enum ProductGender
    {
        MALE,
        FEMALE,
        UNISEX
    }

    public enum ProductStatus
    {
        DELETE,
        PROCESS,
        PAUSE
    }

    public enum ProductCategory
    {
        TOPS,
        BOTTOMS,
        DRESSES,
        OUTERWEAR,
        ACTIVEWEAR,
        LOUNGEWEAR,
        SWIMWEAR,
        SHOES,
        ACCESSORIES,
        BAGS,
        JEWELRY,
        HATS,
        SCARVES,
        BELTS,
        SUNGLASSES,
        OTHER,
        FASHION
    }

    public enum ProductTag
    {
        HOT,
        NEW_ARRIVAL,
        BESTSELLER,
        LIMITED_EDITION,
        SALE,
        EXCLUSIVE
    }

    public enum ProductSortOption
    {
        NEWEST,
        PRICE_LOW_TO_HIGH,
        PRICE_HIGH_TO_LOW,
        BESTSELLING,
        CUSTOMER_RATING
    }

    public enum ProductSize
    {
        XS,
        S,
        M,
        L,
        XL,
        XXL,
        XXXL
    }
}
