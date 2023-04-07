using bloxrocket.auth.Endpoints;

var Builder = WebApplication.CreateBuilder(args);

Builder.Services.AddEndpointsApiExplorer();
Builder.Services.AddSwaggerGen();

Builder.Services.AddCors(CorsOpt =>
{
    CorsOpt.AddDefaultPolicy((policy) =>
    {
        policy.AllowAnyOrigin();
    });
});

var App = Builder.Build();

if (App.Environment.IsDevelopment())
{
    App.UseSwagger();
    App.UseSwaggerUI();
}

new Auth(App);

App.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
        context.Context.Response.Headers.Add("Expires", "-1");
    }
});

App.UseCors();
App.Run();