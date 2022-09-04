using Models.models;

namespace fakeModelsService
    {
    public static class FakeModels
        {
        public static CatalogItem CreateFakeCatalogItem ( )
            {
            return new CatalogItem
                {
                Name = Faker.Name.First ( ),
                Price = Faker.RandomNumber.Next ( ),
                Description = Faker.Lorem.Paragraph ( ),
                ImageLocation = Faker.Internet.Url ( )
                };
            }
        public static CatalogItem CreateFakeCatalogItemById ( Guid id )
            {
            return new CatalogItem
                {
                Name = Faker.Name.First ( ),
                Id = id,
                Price = Faker.RandomNumber.Next ( ),
                Description = Faker.Lorem.Paragraph ( ),
                ImageLocation = Faker.Internet.Url ( )
                };
            }
        }
    }