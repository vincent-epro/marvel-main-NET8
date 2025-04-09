using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace marvel_main_NET8
{
    public static class ValidateClass
    {
        // JWT
        private static readonly string Secret = Environment.GetEnvironmentVariable("JWT_Secret") ?? "";


        public static string GenerateToken(string P_Username)
        {
            byte[] _non_base64_secret = Convert.FromBase64String(Secret);
            SymmetricSecurityKey _symmetric_security_key = new SymmetricSecurityKey(_non_base64_secret);

            ClaimsIdentity _claims_identity = new ClaimsIdentity();
            _claims_identity.AddClaim(new Claim(ClaimTypes.Name, P_Username));

            SecurityTokenDescriptor _security_token_descriptor = new SecurityTokenDescriptor
            {
                Subject = _claims_identity,
                Expires = DateTime.UtcNow.AddMinutes(60 * 24),
                SigningCredentials = new SigningCredentials(_symmetric_security_key, SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityTokenHandler _jwt_security_token_handler = new JwtSecurityTokenHandler();
            JwtSecurityToken _jwt_security_token = _jwt_security_token_handler.CreateJwtSecurityToken(_security_token_descriptor);
            return _jwt_security_token_handler.WriteToken(_jwt_security_token);
        }

        public static string? ValidateToken(string P_Token)
        {
            ClaimsIdentity? _claims_identity;

            ClaimsPrincipal? _claims_principal = GetClaimsPrincipal(P_Token);
            if (_claims_principal == null) return null;


            _claims_identity = (ClaimsIdentity?)_claims_principal.Identity;


            Claim? _claim_name = _claims_identity?.FindFirst(ClaimTypes.Name);
            return _claim_name?.Value; // username
        }

        public static ClaimsPrincipal? GetClaimsPrincipal(string P_Token)
        {
            try
            {
                JwtSecurityTokenHandler _jwt_security_token_handler = new JwtSecurityTokenHandler();
                JwtSecurityToken _jwt_security_token = (JwtSecurityToken)_jwt_security_token_handler.ReadToken(P_Token);

                if (_jwt_security_token == null) return null;

                byte[] _non_base64_secret = Convert.FromBase64String(Secret);

                TokenValidationParameters _token_validation_parameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(_non_base64_secret)
                };

                SecurityToken _security_token;
                ClaimsPrincipal _claims_principal = _jwt_security_token_handler.ValidateToken(P_Token, _token_validation_parameters, out _security_token);

                return _claims_principal;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool Authenticated(string token, string P_Username)
        {
            if (string.IsNullOrEmpty(token) ||
                string.IsNullOrEmpty(P_Username))
            {
                return false;
            }

            return ValidateToken(token) == P_Username;

        }



    }
}
