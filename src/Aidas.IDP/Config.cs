// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Aidas.IDP
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> Ids =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResource(
                    "roles",
                    "Your role(s)",
                    new []{ "role"}),
                new IdentityResource(
                    "country",
                    "The country you live in",
                    new []{ "country"}),
                new IdentityResource(
                    "subscriptionlevel",
                    "Your subscription level",
                    new []{ "subscriptionlevel"}),
            };

        public static IEnumerable<ApiResource> Apis =>
            new ApiResource[]
            {
                new ApiResource("imagegalleryapi", "Image Gallery API", new []{"role"})
                {
                    ApiSecrets = {new Secret("apisecret".Sha256())}
                },
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    AccessTokenType = AccessTokenType.Reference, //default Jwt probably more secure but goes to IDP on each request

                    //IdentityTokenLifetime = 300, //default 5min
                    //AuthorizationCodeLifetime =  300, //default 5min
                    AccessTokenLifetime = 120, //default 1hour int in seconds
                    AllowOfflineAccess = true, //enabling refresh tokens for client
                    UpdateAccessTokenClaimsOnRefresh = true,
                   
                    //AbsoluteRefreshTokenLifetime = 2592000, //default 30days apsoliuti reiksme
                    //RefreshTokenExpiration = TokenExpiration.Sliding, //defult absolute
                    //SlidingRefreshTokenLifetime = 129600, //default 15days niekada nepasieks apsoliutaus expiration lifetime
                    
                    ClientName="Image Gallery",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = { GrantType.AuthorizationCode },
                    RequirePkce = true,
                    RedirectUris = new []
                    {
                        "https://localhost:44389/signin-oidc"
                    },
                    PostLogoutRedirectUris = new []
                    {
                        "https://localhost:44389/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address,
                        "roles",
                        "imagegalleryapi",
                        "country",
                        "subscriptionlevel",
                    },
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    }
                }
            };

    }
}