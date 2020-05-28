﻿using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.IO;

namespace Volo.Abp.BlobStoring.FileSystem
{
    public class FileSystemBlobProvider : BlobProviderBase, ITransientDependency
    {
        protected IBlogFilePathCalculator FilePathCalculator { get; }
        
        public FileSystemBlobProvider(IBlogFilePathCalculator filePathCalculator)
        {
            FilePathCalculator = filePathCalculator;
        }
        
        public override async Task SaveAsync(BlobProviderSaveArgs args)
        {
            var filePath = FilePathCalculator.Calculate(args);

            if (!args.OverrideExisting && await ExistsAsync(filePath))
            {
                throw new BlobAlreadyExistsException($"Saving BLOB '{args.BlobName}' does already exists in the container '{args.ContainerName}'! Set {nameof(args.OverrideExisting)} if it should be overwritten.");
            }
            
            DirectoryHelper.CreateIfNotExists(Path.GetDirectoryName(filePath));

            var fileMode = args.OverrideExisting
                ? FileMode.Create
                : FileMode.CreateNew;
            
            using (var fileStream = File.Open(filePath, fileMode, FileAccess.Write))
            {
                await args.BlobStream.CopyToAsync(
                    fileStream,
                    args.CancellationToken
                );

                await fileStream.FlushAsync();
            }
        }

        public override Task<bool> DeleteAsync(BlobProviderDeleteArgs args)
        {
            var filePath = FilePathCalculator.Calculate(args);
            return Task.FromResult(FileHelper.DeleteIfExists(filePath));
        }

        public override Task<bool> ExistsAsync(BlobProviderExistsArgs args)
        {
            var filePath = FilePathCalculator.Calculate(args);
            return ExistsAsync(filePath);
        }

        public override Task<Stream> GetOrNullAsync(BlobProviderGetArgs args)
        {
            var filePath = FilePathCalculator.Calculate(args);

            if (!File.Exists(filePath))
            {
                return Task.FromResult<Stream>(null);
            }

            return Task.FromResult<Stream>(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        
        protected virtual Task<bool> ExistsAsync(string filePath)
        {
            return Task.FromResult(File.Exists(filePath));
        }
    }
}