using System.Buffers.Text;
using System.Security.Cryptography;
using PlanningPoker.Common.Constants;

namespace PlanningPoker.Service.Services;

public class SessionTokenGenerator : ISessionTokenGenerator
{
    public string Generate()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SessionTokenConstants.ByteLength));
    }
}
