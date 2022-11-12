using System;
using Constructs;
using HashiCorp.Cdktf;
using HashiCorp.Cdktf.Providers.Aws;
using HashiCorp.Cdktf.Providers.Aws.Ec2;
using HashiCorp.Cdktf.Providers.Docker;

namespace MyCompany.MyApp
{
    class MyApp : TerraformStack
    {
        public MyApp(Construct scope, string id) : base(scope, id)
        {
            new AwsProvider(this, "AWS", new AwsProviderConfig { Region = "eu-north-1" });

            Instance instance = new Instance(this, "compute", new InstanceConfig
            { 
                Ami = "ami-01456a894f71116f2",
                InstanceType = "t2.micro",
            });

            new TerraformOutput(this, "public_ip", new TerraformOutputConfig
            {
                Value = instance.PublicIp
            });
        }

        public static void Main(string[] args)
        {
            App app = new App();
            MyApp stack = new MyApp(app, "learn-cdktf-csharp-cloud");
            new RemoteBackend(
                stack,
                new RemoteBackendProps
                {
                    Hostname = "app.terraform.io",
                    Organization = "bob",
                    Workspaces = new NamedRemoteWorkspace("learn-cdktf")
                }
            );

            app.Synth();
        }
    }
}