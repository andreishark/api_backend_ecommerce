using System.Security;
using Models.models;

namespace fakeModelsService
{
    public static class FakeModels
    {

        public static List<string> CreateManyImageLocations ( )
        {
            var count = Faker.RandomNumber.Next ( 1, 20 );
            var imageList = new List<string> ( );

            for ( int i = 0; i < count; i++ )
                imageList.Add ( Faker.Internet.Url ( ) );

            return imageList;
        }

        public static CatalogItem CreateFakeCatalogItem ( )
        {
            return new CatalogItem
            {
                Name = Faker.Name.First ( ),
                Price = Faker.RandomNumber.Next ( ),
                Description = Faker.Lorem.Paragraph ( ),
                ImageLocation = CreateManyImageLocations ( )
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
                ImageLocation = CreateManyImageLocations ( )
            };
        }
    }
}