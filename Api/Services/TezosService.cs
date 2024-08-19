using Netezos.Keys;
using Netezos.Rpc;
using DigipetApi.Api.Models;
using System.Numerics;
using Netezos.Encoding;
using Netezos.Contracts;
using Netezos.Forging.Models;
using Netezos.Forging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace DigipetApi.Api.Services;

public class TezosService
{
    private readonly TezosRpc _rpc;
    private readonly Key _adminKey;
    private readonly string _contractAddress;
    private readonly ILogger<TezosService> _logger;
    private readonly HttpClient _httpClient;

    public TezosService(IConfiguration configuration, ILogger<TezosService> logger, HttpClient httpClient)
    {
        var rpcUrl = configuration["Tezos:RpcUrl"];
        var adminPrivateKey = configuration["Tezos:AdminPrivateKey"];
        _contractAddress = configuration["Tezos:ContractAddress"];

        _rpc = new TezosRpc(rpcUrl);
        _adminKey = Key.FromBase58(adminPrivateKey);
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<string> MintPet(Pet pet)
    {
        try
        {
            _logger.LogInformation($"Starting to mint pet: {pet.PetId}");

            var head = await _rpc.Blocks.Head.Hash.GetAsync<string>();
            _logger.LogInformation($"Retrieved head block hash: {head}");

            var counter = await _rpc.Blocks.Head.Context.Contracts[_adminKey.PubKey.Address].Counter.GetAsync<int>();
            _logger.LogInformation($"Retrieved counter: {counter}");

            var script = await _rpc.Blocks.Head.Context.Contracts[_contractAddress].Script.GetAsync();
            _logger.LogInformation("Retrieved contract script");

            var code = Micheline.FromJson(script.code);
            var cs = new ContractScript(code);

            var param = cs.BuildParameter(
                "mint",
                new
                {
                    name = pet.Name,
                    species = pet.Species,
                    pet_type = pet.Type
                });
            _logger.LogInformation($"Built parameter for minting: {param}");

            var tx = new TransactionContent
            {
                Source = _adminKey.PubKey.Address,
                Counter = ++counter,
                GasLimit = 727,
                StorageLimit = 794,
                Fee = 941,
                Amount = 0,
                Destination = _contractAddress,
                Parameters = new Parameters
                {
                    Entrypoint = "mint",
                    Value = param
                }
            };
            _logger.LogInformation($"Created transaction: {Newtonsoft.Json.JsonConvert.SerializeObject(tx)}");

            var forgedBytes = await new LocalForge().ForgeOperationAsync(head, tx);
            _logger.LogInformation("Forged operation");

            var signature = _adminKey.SignOperation(forgedBytes);
            _logger.LogInformation("Signed operation");

            var signatureBytes = signature.ToBytes();
            var opHash = await _rpc.Inject.Operation.PostAsync(forgedBytes.Concat(signatureBytes));
            _logger.LogInformation($"Injected operation. Operation hash: {opHash}");

            // Wait for confirmation
            var confirmed = await WaitForConfirmation(opHash);
            if (!confirmed)
            {
                throw new Exception($"Operation {opHash} was not confirmed after multiple attempts");
            }
            _logger.LogInformation($"Operation confirmed: {opHash}");

            return opHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MintPet method");
            throw;
        }
    }

    private async Task<bool> WaitForConfirmation(string opHash, int maxAttempts = 20)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var operations = await _rpc.Blocks.Head.Operations.GetAsync();

                foreach (var list in operations)
                {
                    foreach (var op in list)
                    {
                        if (op.hash == opHash)
                        {
                            if (op.contents != null && op.contents.Count > 0)
                            {
                                var status = op.contents[0].metadata?.operation_result?.status;
                                if (status == "applied")
                                {
                                    _logger.LogInformation($"Operation {opHash} confirmed and applied");
                                    return true;
                                }
                                else
                                {
                                    _logger.LogWarning($"Operation {opHash} found but status is {status}");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error checking operation status: {ex.Message}");
            }

            _logger.LogInformation($"Waiting for operation {opHash} to be confirmed. Attempt {i + 1} of {maxAttempts}");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        _logger.LogWarning($"Operation {opHash} not confirmed after {maxAttempts} attempts");
        return false;
    }
}