using Application.Common.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auth
{
    public interface IJwtTokenService
    {
        AccessTokenResult GenerateAccessToken(TokenUserInfo user);
    }
}
