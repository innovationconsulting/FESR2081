using ICWebApp.Application.Services;
using ICWebApp.Application.Interface.Services;
using ICWebApp.DataStore;
using ICWebApp.DataStore.MSSQL.Interfaces;
using ICWebApp.DataStore.MSSQL.Repositories;
using ICWebApp.Application.Interface.Provider;
using ICWebApp.Application.Provider;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using ICWebApp.Application.Helper;
using Microsoft.AspNetCore.Components.Authorization;
using Telerik.Blazor.Services;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using ICWebApp.Classes.Telerik;
using Microsoft.Extensions.Options;
using ICWebApp.DataStore.MSSQL;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ICWebApp.Application.Interface.Helper;
using Blazored.SessionStorage;
using Microsoft.Extensions.FileProviders;
using ICWebApp.Application.Interface.Sessionless;
using ICWebApp.Application.Sessionless;
using ICWebApp.Domain.DBModels;
using Stripe;
using ICWebApp.Application.Interface;
using ICWebApp.DataStore.PagoPA.Interface;
using ICWebApp.DataStore.PagoPA.Repository;
using Microsoft.AspNetCore.Identity;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using ICWebApp.DataStore.MSSQL.Interfaces.UnitOfWork;
using Microsoft.AspNetCore.SignalR;
using ICWebApp.Application.Settings;
using Syncfusion.Blazor;
using ICWebApp.Classes.Syncfusion;
using ICWebApp.Application.Interface.Cache;
using ICWebApp.Application.Cache;
using Microsoft.AspNetCore.Http.Connections;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using ICWebApp.DataStore.MSSQL.UnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddTelerikBlazor();

builder.Services.AddLocalization();

builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });
builder.Services.AddSyncfusionBlazor(options => { options.IgnoreScriptIsolation = false; });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();

builder.Services.AddAuthorization(config =>
{
    config.AddPolicy("Backend", policy =>
        policy.Requirements.Add(new AuthorizationRoleHelper( new List<Guid>() { AuthRoles.Employee, AuthRoles.Administrator })));
    config.AddPolicy("Citizen", policy =>
        policy.Requirements.Add(new AuthorizationRoleHelper(new List<Guid>() { AuthRoles.Citizen })));
});

//CORS
builder.Services.AddCors(corsOption => corsOption.AddDefaultPolicy(
  corsBuilder =>
  {
      corsBuilder.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader(); 
  }));

builder.Services.Configure<RequestLocalizationOptions>(options => {
    var supportedCultures = new List<CultureInfo>()
        {
            new CultureInfo("de-DE"),
            new CultureInfo("it-IT")
        };

    options.DefaultRequestCulture = new RequestCulture("de-DE");

    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

//DBConnection
builder.Services.AddSingleton<DBContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

//Cache
builder.Services.AddSingleton<ITEXTProviderCache, TEXTProviderCache>();
builder.Services.AddSingleton<ILANGProviderCache, LANGProviderCache>();

//Services
builder.Services.AddScoped<ISessionWrapper, SessionWrapper>();
builder.Services.AddScoped<IDialogService, DialogService>();
builder.Services.AddScoped<IBusyIndicatorService, BusyIndicatorService>();
builder.Services.AddScoped<IAccountService, ICWebApp.Application.Services.AccountService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMailerService, MailerService>();
builder.Services.AddScoped<ISMSService, SMSService>();
builder.Services.AddScoped<IPushService, PushService>();
builder.Services.AddScoped<IEnviromentService, EnviromentService>();
builder.Services.AddScoped<IBreadCrumbService, BreadCrumbService>();
builder.Services.AddScoped<IAnchorService, AnchorService>();
builder.Services.AddScoped<IVeriffService, VeriffService>();
builder.Services.AddScoped<ISignService, SignService>();
builder.Services.AddScoped<ISignResponseService, SignResponseService>();
builder.Services.AddScoped<IGeoService, GeoService>();
builder.Services.AddScoped<IPAYService, PAYService>();
builder.Services.AddScoped<IFreshDeskService, FreshDeskService>();
builder.Services.AddScoped<INEWSService, NEWSService>();
builder.Services.AddScoped<ITASKService, TASKService>();
builder.Services.AddScoped<ISPIDService, SPIDService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IMyCivisService, MyCivisService>();
builder.Services.AddScoped<IActionBarService, ActionBarService>();
builder.Services.AddScoped<IFormApplicationService, FormApplicationService>();

//Repositories
builder.Services.AddScoped<IPagoPaRepository, PagoPaRepository>();

//Provider
builder.Services.AddScoped<IAUTHProvider, AUTHProvider>();
builder.Services.AddScoped<IAUTHSettingsProvider, AUTHSettingsProvider>();
builder.Services.AddScoped<IMSGProvider, MSGProvider>();
builder.Services.AddScoped<ILANGProvider, LANGProvider>();
builder.Services.AddScoped<ITEXTProvider, TEXTProvider>();
builder.Services.AddScoped<ICONFProvider, CONFProvider>();
builder.Services.AddScoped<ICANTEENProvider, CANTEENProvider>();
builder.Services.AddScoped<ISYSProvider, SYSProvider>();
builder.Services.AddScoped<IFORMDefinitionProvider, FORMDefinitionProvider>();
builder.Services.AddScoped<IFORMApplicationProvider, FORMApplicationProvider>();
builder.Services.AddScoped<INEWSProvider, NEWSProvider>();
builder.Services.AddScoped<IFILEProvider, FILEProvider>();
builder.Services.AddScoped<IRoomProvider, RoomProvider>();
builder.Services.AddScoped<IINFO_PAGEProvider, INFO_PAGEProvider>();
builder.Services.AddScoped<ISETTProvider, SETTProvider>();
builder.Services.AddScoped<IPAYProvider, PAYProvider>();
builder.Services.AddScoped<IDASHProvider, DASHProvider>();
builder.Services.AddScoped<IPRIVProvider, PRIVProvider>();
builder.Services.AddScoped<IORGProvider, ORGProvider>();
builder.Services.AddScoped<IAPPProvider, APPProvider>();
builder.Services.AddScoped<IMETAProvider, METAProvider>();
builder.Services.AddScoped<ITASKProvider, TASKProvider>();
builder.Services.AddScoped<ICONTProvider, CONTProvider>();

//Sessionles
builder.Services.AddScoped<IFORMApplicationSessionless, FORMApplicationSessionless>();
builder.Services.AddScoped<ISignResponseSessionless, SignResponseSessionless>();
builder.Services.AddScoped<ICONFProviderSessionless, CONFProviderSessionless>();


//Helper
builder.Services.AddScoped<AuthenticationStateProvider, AuthenticationHelper>();
builder.Services.AddScoped<IAuthorizationHandler, AuthorizationRoleHelperHandler>();
builder.Services.AddScoped<IFORM_ReportRendererHelper, FORM_ReportRendererHelper>();
builder.Services.AddScoped<IFORM_ReportPrintHelper, FORM_ReportPrintHelper>();
builder.Services.AddScoped<IFormBuilderHelper, FormBuilderHelper>();
builder.Services.AddScoped<IFormAdministrationHelper, FormAdministrationHelper>();
builder.Services.AddScoped<ICanteenAdministrationHelper, CanteenAdministrationHelper>();
builder.Services.AddScoped<IRoomAdministrationHelper, RoomAdministrationHelper>();
builder.Services.AddScoped<ISpidHelper, SpidHelper>();
builder.Services.AddScoped<IFormRendererHelper, FormRendererHelper>();
builder.Services.AddScoped<INavMenuHelper, NavMenuHelper>();
builder.Services.AddScoped<IRoomGalerieHelper, RoomGalerieHelper>();
builder.Services.AddScoped<IRequestAdministrationHelper, RequestAdministrationHelper>();
builder.Services.AddScoped<ICreateFormDefinitionHelper, CreateFormDefinitionHelper>();
builder.Services.AddScoped<IRoomBookingHelper, RoomBookingHelper>();
builder.Services.AddScoped<ID3Helper, D3Helper>();
builder.Services.AddScoped<IMunicipalityHelper, MunicipalityHelper>();

//Telerik Localization
builder.Services.AddScoped(typeof(ITelerikStringLocalizer), typeof(TelerikResxLocalizer));
builder.Services.AddScoped(typeof(ISyncfusionStringLocalizer), typeof(SyncfusionLocalizer));

builder.Services.RegisterIntlTelInput();

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredUniqueChars = 1;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 10240; // 1MB
});


builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("");

var app = builder.Build();

app.UseExceptionHandler("/Spid/SpidError");
app.UseWebSockets();

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "D:/Comunix/NewsImages")),
    RequestPath = "/NewsImages"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "D:/Comunix/Privacy")),
    RequestPath = "/Privacy"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "D:/Comunix/RoomImages")),
    RequestPath = "/RoomImages"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "D:/Comunix/Sign" )),
        RequestPath = "/SignDocs"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "D:/Comunix/MailContent")),
    RequestPath = "/Content"
});
app.UseRouting();

app.UseRequestLocalization(options => {
    var supportedCultures = new List<CultureInfo>()
        { 
            new CultureInfo("de-DE"),
            new CultureInfo("it-IT")
        };

    options.DefaultRequestCulture = new RequestCulture("de-DE");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseEndpoints(endpoints => { 
    endpoints.MapControllers();
});

app.MapBlazorHub(configureOptions: options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
    options.WebSockets.CloseTimeout = new TimeSpan(1, 1, 1);
});

app.MapFallbackToPage("/App/_Host");

app.Run();
