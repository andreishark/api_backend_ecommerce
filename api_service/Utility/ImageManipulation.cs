namespace api_service.Utility;
public static class ImageManipulation
{
    static public async Task<List<string>?> WriteImagesToDiskAsync ( string staticImagesPathDisk, string staticImagesPathApi, ILogger _logger, IFormFile [ ] files, Guid product_id )
    {
        var apiPaths = new List<string> ( );

        for ( int i = 0; i < files.Length; i++ )
        {
            var image_id = Guid.NewGuid ( );
            var file = files [ i ];

            _logger.LogInformation ( "Creating apiPath for image" );
            var extension = new FileInfo ( file.FileName ).Extension;
            var file_name = $"{image_id}{extension}";


            apiPaths.Add ( Path.Combine ( staticImagesPathApi, product_id.ToString ( ), file_name ) );

            _logger.LogInformation ( "Trying to upload image to disk" );
            try
            {

                string filePath = Path.Combine ( staticImagesPathDisk, product_id.ToString ( ), file_name );
                var fileInfo = new FileInfo ( filePath );

                if ( fileInfo is null )
                {
                    _logger.LogCritical ( "Unexpected error in creating file info" );
                    return null;
                }

                if ( fileInfo.DirectoryName is null )
                {
                    _logger.LogCritical ( "Unexpected error in creating file info" );
                    return null;
                }

                if ( !Directory.Exists ( fileInfo.DirectoryName ) )
                {
                    _logger.LogInformation ( "Directory does not exist, creating..." );
                    Directory.CreateDirectory ( fileInfo.DirectoryName );
                }

                using Stream fileStream = new FileStream ( filePath, FileMode.Create );
                await file.CopyToAsync ( fileStream );
            }
            catch ( System.Exception ex )
            {
                _logger.LogError ( "File couldn't be written to disk.\nError: {}", ex.Message );
                _logger.LogTrace ( ex.StackTrace );
                return null;
            }
        }
        _logger.LogInformation ( "Files uploaded successfully." );

        return apiPaths;
    }

    static public List<string>? DeleteImagesFromDisk ( string staticImagesPathDisk, ILogger _logger, List<string> deletePaths, List<string> apiPaths, Guid product_id, bool archived = true )
    {
        for ( int i = 0; i < deletePaths.Count; i++ )
        {
            var deletedProduct = deletePaths [ i ];
            var imageId = Path.GetFileName ( deletedProduct );

            var filePath = Path.Combine ( staticImagesPathDisk, product_id.ToString ( ), imageId );
            var newFilePath = Path.Combine ( staticImagesPathDisk, product_id.ToString ( ), $"_{imageId}" );

            if ( apiPaths.Contains ( deletedProduct ) )
            {
                try
                {
                    var fileInfo = new FileInfo ( filePath );
                    var newFileInfo = new FileInfo ( newFilePath );

                    if ( fileInfo is null || newFileInfo is null )
                    {
                        _logger.LogCritical ( "fileInfo is null" );
                        throw new NullReferenceException ( "FileInfo or newFileInfo is null" );
                    }
                    if ( archived )
                        System.IO.File.Move ( fileInfo.FullName, newFileInfo.FullName );
                    System.IO.File.Delete ( fileInfo.FullName );

                }
                catch ( System.Exception ex )
                {
                    _logger.LogError ( "Couldn't read file or rename it.\nError: {}", ex.Message );
                    _logger.LogTrace ( ex.StackTrace );
                    return null;
                }

                _logger.LogInformation ( "Archived image {0}", deletedProduct );
                apiPaths.Remove ( deletedProduct );
            }
            else
            {
                _logger.LogWarning ( "Element {0} not found", deletedProduct );
            }
            _logger.LogInformation ( "{} image iterated", i );
        }

        return apiPaths;
    }
}