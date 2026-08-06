# Introduction 
The Microsoft Power Platform offers a variety of possibilities to "lend a hand" and to develop adaptations in Dynamics 365 or your own Power Apps. Without structure, this procedure can quickly become confusing for developers and IT departments. There are a variety of tools that can support the developer in his efforts but no central place where everything comes together.

***apptech Power CID Portal*** is a web portal developed by applied technologies that supports users of the Microsoft Power Platform in creating, testing and deploying self-developed or customised apps and components from Dataverse (formerly "Common Data Service") environment (formerly "Instances") to Dataverse environment within a Microsoft 365 tenant and to follow this process. Power CID follows Microsoft's Power Platform Application Lifecycle Management (ALM).

![Screenshot 2022-05-13 142020](https://user-images.githubusercontent.com/65557623/168282959-621b386f-449b-4426-b691-75dae25e678c.png)

# Functionality

The Microsoft Power Platform is a low-code platform from Microsoft, which combines various services in the area of business applications, such as Power Apps (user interfaces for interacting with data sources) and Power Automate (process and task automation). Microsoft Dataverse is a central component of data storage. In order to be able to transport adjustments within these services, for example applications developed using Power Apps, from the respective development to the integration, test and production environment, these must be exported manually as ZIP archives by default and then imported into the target environment. To do this, the user must switch between the environments manually. This procedure can be seen as above-average time-consuming and error-prone, especially for inexperienced users. In addition, there is no overview of the adjustments made (so-called patches and upgrades) and therefore no possibility to restore older versions afterwards. Various rights are required for import and export within the Microsoft Power Platform environment, which are more extensive than would be necessary for regular use. This can be considered a security risk.

The independent web application *apptech Power CID Portal* is intended to eliminate these vulnerabilities and offers both the possibility to create adjustments in Microsoft Dataverse and to transfer them to other environments more easily than in the standard procedure described above. Manual handling of ZIP archives is no longer necessary. The web application also offers a clear and central depiction for tracking changes to the managed applications. It is also possible to download old versions of managed applications.

![Screenshot 2022-05-13 161014](https://user-images.githubusercontent.com/65557623/168302305-221d4aa3-8ffd-473b-844f-ab874f4c6dd9.png)

# Technical Implementation

The application uses the Microsoft Authentication Library (MSAL) in connection with the Microsoft identity platform. This enables a login in the *apptech Power CID Portal* with company accounts of any Microsoft tenant. For authorized access to the Power Platform environments of a tenant, the respective administrator must initially add and authorize an application user when setting up a tenant within the web application. This process is executed independently by the *apptech Power CID Portal* in the user context of the administrator to ensure the best possible user experience. The interaction between the *apptech Power CID Portal* and the interfaces of the Microsoft Power Platform or the Microsoft Dataverse takes place via HTTP REST requests.

<img width="547" alt="Power CID Portal Schaubild Aufbau" src="https://user-images.githubusercontent.com/65557623/168295895-b915d357-4f39-4464-9344-50c5e170b02b.png">

The web application itself consists of a server-side and a client-side part. The Angular front-end framework based on the TypeScript programming language is used in the client. On the server side, the ASP.NET Framework is used to provide a REST API based on the Open Data Protocol (OData). The UI component framework DevExtreme is also used to design the user interface (buttons, tables, etc.).

# Cross-Tenant Delivery

*apptech Power CID Portal* can optionally deliver solutions across tenant boundaries: a solution version can be marked as *released for external delivery* by the vendor tenant and then deployed into Dataverse environments that live in other (customer) Microsoft 365 tenants. This capability is disabled by default and only available to a single, explicitly configured vendor tenant.

## Setup

1. **Azure AD App Roles** - add the following two application roles to the app registration manifest, alongside the existing `atPowerCID.Admin` / `atPowerCID.Manager` / `atPowerCID.User` roles:
   - `atPowerCID.ExternalReleaseManager` - may release/unrelease solution versions for external delivery and manage external environments and external deployment paths.
   - `atPowerCID.ExternalDeployer` - may deploy externally released solutions into a foreign tenant they are permitted for.
2. **Server configuration** (`appsettings.json`):
   - `AppRoleIds:ExternalReleaseManager` / `AppRoleIds:ExternalDeployer` - the GUIDs of the two app roles created above.
   - `CrossTenantDelivery:AllowedTenantId` - the Azure AD tenant ID of the single ("vendor") tenant that is allowed to use cross-tenant delivery. Leave empty to keep the feature disabled.
3. **Client configuration** (`ClientApp/src/assets/config/config.*.json`):
   - `azure.appRoleIds.externalReleaseManager` / `azure.appRoleIds.externalDeployer` - same GUIDs as above.
4. **Register external environments** - as an `Admin` or `ExternalReleaseManager` of the vendor tenant, use *App Settings > External Environments* to register environments of a foreign (customer) tenant. Customer environments must already exist as regular environments in the portal's database, and the portal's application user must already be authorized on them, exactly like for internal environments.
5. **Create external deployment paths** - use *App Settings > External Deployment Paths* to create ordered delivery paths across the registered external environments of one foreign tenant, and assign applications to them (via the application's "Assign external deployment paths" action).
6. **Grant deploy permissions** - in *Users*, use "Manage external tenant permissions" to grant an `ExternalDeployer` user permission to deploy into a specific foreign tenant.
7. **Release and deploy** - release a solution version for external delivery from its details popup, then deploy it from the new *External Delivery* area.

## Manual test checklist

- Releasing/un-releasing a solution version for external delivery (visible only to `Admin`/`ExternalReleaseManager` of the vendor tenant).
- Registering, aliasing, deactivating and removing an external environment.
- Creating an external deployment path, adding/reordering/removing steps, and enforcing that all steps belong to the same foreign tenant.
- Assigning/reordering external deployment paths on an application.
- Granting/revoking a user's external tenant deploy permission.
- Deploying a released solution version and confirming the resulting action appears only in the vendor tenant's history (not in the customer tenant's), with the "External" indicator set.
- Confirming none of the cross-tenant screens, actions or roles are reachable from a tenant other than the configured `CrossTenantDelivery:AllowedTenantId`.

