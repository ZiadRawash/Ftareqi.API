using Ftareqi.Application.DTOs.Cloudinary;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Domain.Enums;
using Microsoft.AspNetCore.Http;

public class FileMapper : IFileMapper
{
    public List<CloudinaryReqDto> MapFilesWithTypes(List<IFormFile> files, List<ImageType> imageTypes)
    {
        if (files.Count != imageTypes.Count)
            throw new ArgumentException("Number of files must match number of image types.");

        var result = new List<CloudinaryReqDto>();

        for (int i = 0; i < files.Count; i++)
            result.Add(MapFile(files[i], imageTypes[i]));

        return result;
    }

    public CloudinaryReqDto MapFile(IFormFile file, ImageType imageType)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        // Generate a safe temporary path
        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}"
        );

        using (var stream = new FileStream(tempFilePath, FileMode.Create))
            file.CopyTo(stream);

        return new CloudinaryReqDto
        {
            TempFilePath = tempFilePath,
            FileName = file.FileName,
            Type = imageType
        };
    }

    public List<CloudinaryReqDto> MapFilesWithTypes(List<(IFormFile File, ImageType Type)> inputs)
    {
        var result = new List<CloudinaryReqDto>();

        foreach (var (File, Type) in inputs)
            result.Add(MapFile(File, Type));

        return result;
    }
}
