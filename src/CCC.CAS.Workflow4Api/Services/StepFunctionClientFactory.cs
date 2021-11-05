using Amazon.StepFunctions;
using System;

public static class StepFunctionClientFactory
{
    public static AmazonStepFunctionsClient GetClient() {
          // doc is here. I reads your local AWS profile, "default" by default https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/StepFunctions/TStepFunctionsClient.html
          return new AmazonStepFunctionsClient(); 
    }
}
