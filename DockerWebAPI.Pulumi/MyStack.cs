
using System.Collections.Immutable;
using System.Text;
using Pulumi;
using Pulumi.Aws.Ec2.Inputs;
using Docker = Pulumi.Docker;
using Ec2 = Pulumi.Aws.Ec2;
using Ecs = Pulumi.Aws.Ecs;
using Ecr = Pulumi.Aws.Ecr;
using Elb = Pulumi.Aws.LB;
// aws.lb.Listener
// using Elb = Pulumi.Aws.ElasticLoadBalancingV2;
using Iam = Pulumi.Aws.Iam;

class FargateStack : Stack
{
    public FargateStack()
    {
        // Read back the default VPC and public subnets, which we will use.
        var vpcId = Ec2.GetVpc.Invoke(new Ec2.GetVpcInvokeArgs {Default = true})
            .Apply(vpc => vpc.Id);

        var subnetIds = Ec2.GetSubnets.Invoke(new Ec2.GetSubnetsInvokeArgs() 
                { Filters = new InputList<GetSubnetsFilterInputArgs>(){new GetSubnetsFilterInputArgs{Name = "vpc-id", Values = vpcId}} })
            .Apply(s => s.Ids);

        // Create a SecurityGroup that permits HTTP ingress and unrestricted egress.
        var webSg = new Ec2.SecurityGroup("web-sg", new Ec2.SecurityGroupArgs
        {
            VpcId = vpcId,
            Egress =
            {
                new Ec2.Inputs.SecurityGroupEgressArgs
                {
                    Protocol = "-1",
                    FromPort = 0,
                    ToPort = 0,
                    CidrBlocks = {"0.0.0.0/0"}
                }
            },
            Ingress =
            {
                new Ec2.Inputs.SecurityGroupIngressArgs
                {
                    Protocol = "tcp",
                    FromPort = 80,
                    ToPort = 80,
                    CidrBlocks = {"0.0.0.0/0"}
                }
            }
        });

        // Create an ECS cluster to run a container-based service.
        var cluster = new Ecs.Cluster("app-cluster");

        // Create an IAM role that can be used by our service's task.
        var taskExecRole = new Iam.Role("task-exec-role", new Iam.RoleArgs
        {
            AssumeRolePolicy = @"{
""Version"": ""2008-10-17"",
""Statement"": [{
    ""Sid"": """",
    ""Effect"": ""Allow"",
    ""Principal"": {
        ""Service"": ""ecs-tasks.amazonaws.com""
    },
    ""Action"": ""sts:AssumeRole""
}]
}"
        });
        var taskExecAttach = new Iam.RolePolicyAttachment("task-exec-policy", new Iam.RolePolicyAttachmentArgs
        {
            Role = taskExecRole.Name,
            PolicyArn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
        });

        // Create a load balancer to listen for HTTP traffic on port 80.
        var webLb = new Elb.LoadBalancer("web-lb", new Elb.LoadBalancerArgs
        {
            Subnets = subnetIds,
            // SecurityGroups = {webSg.Id},
            LoadBalancerType = "network"
            
        });
        var webTg = new Elb.TargetGroup("web-tg", new Elb.TargetGroupArgs
        {
            Port = 80,
            Protocol = "TCP",
            TargetType = "ip",
            VpcId = vpcId
        });
        var webListener = new  Elb.Listener("web-listener", new Elb.ListenerArgs
        {
            LoadBalancerArn = webLb.Arn,
            Port = 80,
            Protocol = "TCP",
            DefaultActions =
            {
                new Elb.Inputs.ListenerDefaultActionArgs
                {
                    Type = "forward",
                    TargetGroupArn = webTg.Arn,
                }
            }
        });
        // Create a private ECR registry and build and publish our app's container image to it.
        var appRepo = new Ecr.Repository("app-repo");
        var appRepoCredentials = Ecr.GetCredentials
            .Invoke(new Ecr.GetCredentialsInvokeArgs {RegistryId = appRepo.RegistryId})
            .Apply(credentials =>
            {
                var data = Convert.FromBase64String(credentials.AuthorizationToken);
                return Encoding.UTF8.GetString(data).Split(":").ToImmutableArray();
            });
        var image = new Docker.Image("app-img", new Docker.ImageArgs
        {
            Build = "../",
            ImageName = appRepo.RepositoryUrl,
            Registry = new Docker.ImageRegistry
            {
                Server = appRepo.RepositoryUrl,
                Username = appRepoCredentials.GetAt(0),
                Password = appRepoCredentials.GetAt(1)
            }
        });

        // Spin up a load balanced service running our container image.
        var appTask = new Ecs.TaskDefinition("app-task", new Ecs.TaskDefinitionArgs
        {
            Family = "fargate-task-definition",
            Cpu = "256",
            Memory = "512",
            NetworkMode = "awsvpc",
            RequiresCompatibilities = {"FARGATE"},
            ExecutionRoleArn = taskExecRole.Arn,
            ContainerDefinitions = image.ImageName.Apply(imageName => @"[{
""name"": ""my-app"",
""image"": """ + imageName + @""",
""portMappings"": [{
    ""containerPort"": 80,
    ""hostPort"": 80,
    ""protocol"": ""tcp""
}]
}]")
        });
        var appSvc = new Ecs.Service("app-svc", new Ecs.ServiceArgs
        {
            Cluster = cluster.Arn,
            DesiredCount = 1,
            LaunchType = "FARGATE",
            TaskDefinition = appTask.Arn,
            NetworkConfiguration = new Ecs.Inputs.ServiceNetworkConfigurationArgs
            {
                AssignPublicIp = true,
                Subnets = subnetIds,
                SecurityGroups = {webSg.Id}
            },
            LoadBalancers =
            {
                new Ecs.Inputs.ServiceLoadBalancerArgs
                {
                    TargetGroupArn = webTg.Arn,
                    ContainerName = "my-app",
                    ContainerPort = 80
                }
            }
        }, new CustomResourceOptions {DependsOn = {webListener}});
        
        var httpApiGateway = new Pulumi.Aws.ApiGatewayV2.Api("PulumiWebApiGateway_ApiGateway", new Pulumi.Aws.ApiGatewayV2.ApiArgs
        {
            ProtocolType = "HTTP",
            RouteSelectionExpression = "${request.method} ${request.path}",
        });
        
        
        var httpApiGateway_integration = new Pulumi.Aws.ApiGatewayV2.Integration("PulumiWebApiGateway_ApiGatewayIntegration", new Pulumi.Aws.ApiGatewayV2.IntegrationArgs
        {
            ApiId = httpApiGateway.Id,
            // IntegrationType = "HTTP",
            IntegrationType = "HTTP_PROXY",
            IntegrationMethod = "ANY",
            IntegrationUri = webLb.Arn,
            PayloadFormatVersion = "2.0",
            TimeoutMilliseconds = 30000,
            // ConnectionType = "VPC_LINK",
            // ConnectionId = vpcId
        });
        
        var httpApiGatewayRoute = new Pulumi.Aws.ApiGatewayV2.Route("PulumiWebApiGateway_ApiGatewayRoute", new Pulumi.Aws.ApiGatewayV2.RouteArgs
        {
            ApiId = httpApiGateway.Id,
            RouteKey = "$default",
            Target = httpApiGateway_integration.Id.Apply(id => $"integrations/{id}"),
        });
        
        var httpApiGatewayStage = new Pulumi.Aws.ApiGatewayV2.Stage("PulumiWebApiGateway_ApiGatewayStage", new Pulumi.Aws.ApiGatewayV2.StageArgs
        {
            ApiId = httpApiGateway.Id,
            AutoDeploy = true,
            Name = "$default",
        });
        
        // var lambdaPermissionsForApiGateway = new Aws.Lambda.Permission("PulumiWebApiGateway_LambdaPermission", new Aws.Lambda.PermissionArgs
        // {
        //     Action = "lambda:InvokeFunction",
        //     Function = lambdaFunction.Name,
        //     Principal = "apigateway.amazonaws.com",
        //     SourceArn = Output.Format($"{httpApiGateway.ExecutionArn}/*") // note it's the ExecutionArn.
        //     // SourceArn = httpApiGateway.ExecutionArn.Apply(arn => $"{arn}/*") // this is another way of doing the same thing
        // });
        
        this.ApiEndpoint = httpApiGateway.ApiEndpoint.Apply(endpoint =>  $"{endpoint}");
        
        // Export the resulting web address.
        this.Url = Output.Format($"http://{webLb.DnsName}");
    }

    [Output] public Output<string> Url { get; set; }
    [Output] public Output<string> ApiEndpoint { get; set; }
}