# Client-side encryption example for Azure SDKs

This is a simple sample showing how to use a Key Encryption Key for [client-side blob encryption](https://docs.microsoft.com/azure/storage/common/storage-client-side-encryption?tabs=dotnet#blob-service-encryption) using the new Azure SDKs:

* [Azure.Identity](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/identity/Azure.Identity/README.md)
* [Azure.Security.KeyVault.Keys](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/keyvault/Azure.Security.KeyVault.Keys/README.md)
* [Azure.Storage.Blobs](https://github.com/Azure/azure-sdk-for-net/blob/master/sdk/storage/Azure.Storage.Blobs/README.md)

## Getting started

You'll need to install the [Azure CLI](https://aka.ms/docs/azurecli) to run this sample.

1. Log in:

    ```bash
    az login
    ```

2. Create a resource group. Be sure to change the name to something unique. Do not use spaces, dashes, or underscores unless you change the deployment template since both Key Vault and Storage have different name character limits.

    ```bash
    az group create -n rg-keksample -l westus2
    ```

3. Deploy the template to the resource group you just created. The name parameter must also be unique for the same reason described above.

    ```bash
    az deployment group create -g rg-keksample -f deployment.json -p name=keksample
    ```

    The output will contain an `outputs` group with values you'll need to use below, specifically `` and ``. Save them to variables for ease. The syntax below uses bash, so adjust for your shell as appropriate.

    ```bash
    AZURE_KEYVAULT_URL=https://keksamplekv.vault.azure.net/
    STORAGE_CONNECTION_STRING=$(echo "DefaultEndpointsProtocol=https;AccountName=keksamplestg;AccountKey=Nv...;EndpointSuffix=core.windows.net")
    ```

4. Now add an access policy. The example below adds your user account using `--upn`, but you can just as easily add a service principal using `--spn`. See `az keyvault set-policy --help` for more information. Normally you wouldn't let this principal create or update keys or secrets, but we do so here for the purpose of this exampe.

    ```bash
    az keyvault set-policy -n keksamplekv --upn user@domain.com --key-permissions get create update wrapKey unwrapKey --secret-permissions get set
    ```

5. Now create a key or secret. Keys will automatically create a secure key. The key really only need `wrapKey` and `unwrapKey` operation permissions if you don't use it for anything else. If you use secrets for legacy support, you'll need to supply a cryptographically secure key value yourself.

    ```bash
    az keyvault key create --vault-name keksamplekv -n kek --kty RSA --ops wrapKey unwrapKey
    ```

    You can also create an "oct" key type, but will need to change the `KeyWrapAlgorithm` in _Program.cs_ accordingly.

6. Now run the sample program to upload a file like this one. It will be encrypted during upload. Run it again without the `--file` parameter to download and decrypt the file. Note that the file in this sample is output to `stdout` so binary files may exhibit issues. Please use text files for this sample.

    ```bash
    dotnet run -- --key-id $AZURE_KEYVAULT_URL/keys/kek --connection-string $STORAGE_CONNECTION_STRING --container sample --path README.md --file README.md
    dotnet run -- --key-id $AZURE_KEYVAULT_URL/keys/kek --connection-string $STORAGE_CONNECTION_STRING --container sample --path README.md
    ```

7. When you are finished, you can simply delete the resource group you create previously:

    ```bash
    az group delete -n rg-keksample --yes
    ```
