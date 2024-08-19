using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Models;

namespace DigipetApi.Api.Interfaces;

public interface ITezosService
{
  Task<string> MintPet(Pet pet);
  Task<bool> WaitForConfirmation(string opHash, int maxAttempts = 20);

}