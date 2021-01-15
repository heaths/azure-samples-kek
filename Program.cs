// Copyright 2021 Heath Stewart
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

class Program
{
    /// <summary>
    /// Uploads a file using client-side encryption, or downloads and prints an existing path using client-side decryption.
    /// </summary>
    /// <param name="keyId">The URI to a Key Vault key or secret containing your encryption and decryption keys.</param>
    /// <param name="connectionString">The connection string to a blob container.</param>
    /// <param name="container">The name of the blob container.</param>
    /// <param name="path">The path within the connected container to upload or download.</param>
    /// <param name="file">The local path of a file to encrypt and upload.</param>
    static async Task Main(Uri keyId, string connectionString, string container, string path, FileInfo file = null)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (source, args) =>
        {
            Console.Error.WriteLine("Cancelling...");

            cts.Cancel();
            args.Cancel = true;
        };

        var keyResolver = new KeyResolver(new DefaultAzureCredential());
        var kek = await keyResolver.ResolveAsync(keyId, cts.Token);

        var blobClient = new BlobServiceClient(connectionString, new SpecializedBlobClientOptions
        {
            ClientSideEncryption = new ClientSideEncryptionOptions(ClientSideEncryptionVersion.V1_0)
            {
                KeyResolver = keyResolver,
                KeyEncryptionKey = kek,
                KeyWrapAlgorithm = KeyWrapAlgorithm.RsaOaep256.ToString(),
            },
        });

        var blobContainerClient = blobClient.GetBlobContainerClient(container);

        if (file is null)
        {
            Console.Error.WriteLine($"Downloading '{container}/{path}'...");

            await blobContainerClient.GetBlobClient(path).DownloadToAsync(Console.OpenStandardOutput(), cts.Token);
        }
        else
        {
            Console.Error.WriteLine($"Uploading '{file.FullName}' to '{container}/{path}'...");

            await blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cts.Token);
            await blobContainerClient.GetBlobClient(path).UploadAsync(file.FullName, true, cts.Token);

            Console.Error.WriteLine("Done");
        }
    }
}
