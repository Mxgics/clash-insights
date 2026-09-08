using ClashInsights.Api;
using Microsoft.Extensions.Configuration;
using System.Net;
namespace ClashInsights.Tests;
public class ClientFailureTests {
 [Theory][InlineData(403,"{}","Access denied: check key, IP or data visibility")][InlineData(404,"{}","Not found")][InlineData(503,"{}","Upstream unavailable")][InlineData(200,"{","Invalid upstream data")][InlineData(200,"{}","Invalid upstream data")]
 public async Task FailuresDoNotCreateSnapshots(int code,string body,string expected){
  using var http=new HttpClient(new ResponseHandler(code,body)){BaseAddress=new Uri("https://fixture.invalid/")};
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Clash:ApiToken","fixture-only"}}).Build();
  var result=await new ClashClient(http,config).Fetch("players/tag",default);Assert.Equal(expected,result.Status);Assert.Null(result.Json);
 }
 private sealed class ResponseHandler(int code,string body):HttpMessageHandler {
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage((HttpStatusCode)code){Content=new StringContent(body)});
 }
}
