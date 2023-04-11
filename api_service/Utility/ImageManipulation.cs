namespace api_service.Utility;

public static class ImageManipulation
{
    public static async Task<List<string>?> WriteImagesToDiskAsync(string staticImagesPathDisk,
        string staticImagesPathApi,
        ILogger logger,
        IEnumerable<IFormFile> files,
        Guid productId)
    {
        var apiPaths = new List<string>();

        foreach (var t in files)
        {
            var imageId = Guid.NewGuid();

            logger.LogInformation("Creating apiPath for image");
            var extension = new FileInfo(t.FileName).Extension;
            var fileName = $"{imageId}{extension}";


            apiPaths.Add(Path.Combine(staticImagesPathApi, productId.ToString(), fileName));

            logger.LogInformation("Trying to upload image to disk");
            try
            {
                string filePath = Path.Combine(staticImagesPathDisk, productId.ToString(), fileName);
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.DirectoryName is null)
                {
                    logger.LogCritical("Unexpected error in creating file info");
                    return null;
                }

                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    logger.LogInformation("Directory does not exist, creating...");
                    Directory.CreateDirectory(fileInfo.DirectoryName);
                }

                using Stream fileStream = new FileStream(filePath, FileMode.Create);
                await t.CopyToAsync(fileStream);
            }
            catch (System.Exception ex)
            {
                logger.LogError("File couldn't be written to disk.\nError: {}", ex.Message);
                logger.LogTrace(ex.StackTrace);
                return null;
            }
        }

        logger.LogInformation("Files uploaded successfully");

        return apiPaths;
    }

    public static List<string>? DeleteImagesFromDisk(string staticImagesPathDisk,
        ILogger logger,
        List<string> deletePaths,
        List<string> apiPaths,
        Guid productId,
        bool archived = true)
    {
        for (var i = 0; i < deletePaths.Count; i++)
        {
            var deletedProduct = deletePaths[i];
            var imageId = Path.GetFileName(deletedProduct);

            var filePath = Path.Combine(staticImagesPathDisk, productId.ToString(), imageId);
            var newFilePath = Path.Combine(staticImagesPathDisk, productId.ToString(), $"_{imageId}");

            if (apiPaths.Contains(deletedProduct))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var newFileInfo = new FileInfo(newFilePath);

                    if (fileInfo is null || newFileInfo is null)
                    {
                        logger.LogCritical("fileInfo is null");
                        throw new NullReferenceException("FileInfo or newFileInfo is null");
                    }

                    if (archived)
                        File.Move(fileInfo.FullName, newFileInfo.FullName);
                    File.Delete(fileInfo.FullName);
                }
                catch (System.Exception ex)
                {
                    logger.LogError("Couldn't read file or rename it.\nError: {}", ex.Message);
                    logger.LogTrace(ex.StackTrace);
                    return null;
                }

                logger.LogInformation("Archived image {0}", deletedProduct);
                apiPaths.Remove(deletedProduct);
            }
            else
            {
                logger.LogWarning("Element {0} not found", deletedProduct);
            }

            logger.LogInformation("{} image iterated", i);
        }

        return apiPaths;
    }
}