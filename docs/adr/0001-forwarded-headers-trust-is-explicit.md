# Forwarded headers are trusted from loopback unless configured otherwise

The app reads `X-Forwarded-For` and `X-Forwarded-Proto`, which App Service's front end sets after ending TLS. They drive the HTTPS redirect, the Secure Operator cookie, and the client address used by the sign-in lockout and the issues log. Any sender used to be trusted, so a client reaching the app directly could set the headers itself and spoof its address (dodging the lockout) or its scheme.

Decision: a `ForwardedHeaders` configuration section decides who may set them. Unset, only loopback is trusted (the ASP.NET default). `KnownProxies` and `KnownNetworks` add trusted addresses and CIDR networks. `TrustAnySender=true` trusts every sender and is set explicitly in `appsettings.Production.json`, because the front end's address is not known ahead of time and the deployed app is reached through it alone. Only the last hop is read (`ForwardLimit` 1).

Consequences:
- The Azure deployment keeps working because the production defaults say `TrustAnySender: true`; any other host (a container, a VM, a different reverse proxy) is safe by default and lists its proxy.
- Trusting any sender is only sound while nothing but the front end can reach the app. Moving the app somewhere it is directly reachable means setting `TrustAnySender` to false and listing the proxies.
- An unparsable `KnownProxies` or `KnownNetworks` entry stops the host at startup.
