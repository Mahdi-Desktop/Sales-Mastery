using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using AspnetCoreMvcFull.Services.Interface;

namespace AspnetCoreMvcFull.Services
{
  public class FirebaseInitializer
  {
    public static void Initialize(IConfiguration configuration)
    {
      if (FirebaseApp.DefaultInstance == null)
      {
        var serviceAccountKeyPath = configuration["Firebase:ServiceAccountKeyPath"];

        FirebaseApp.Create(new AppOptions()
        {
          Credential = GoogleCredential.FromFile(serviceAccountKeyPath),
          ProjectId = configuration["Firebase:ProjectId"]
        });
      }
    }
  }
}
