﻿namespace MyTested.Mvc.Tests.BuildersTests.ControllersTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using Builders.Contracts.Actions;
    using Builders.Contracts.Base;
    using Exceptions;
    using Microsoft.AspNet.Mvc;
    using Setups;
    using Setups.Controllers;
    using Setups.Models;
    using Setups.Services;
    using Xunit;

    public class ControllerBuilderTests
    {
        [Fact]
        public void CallingShouldPopulateCorrectActionNameAndActionResultWithNormalActionCall()
        {
            var actionResultTestBuilder = MyMvc
                .Controller<MvcController>()
                .Calling(c => c.OkResultAction());

            this.CheckActionResultTestBuilder(actionResultTestBuilder, "OkResultAction");
        }

        [Fact]
        public void CallingShouldPopulateCorrectActionNameAndActionResultWithAsyncActionCall()
        {
            var actionResultTestBuilder = MyMvc
                .Controller<MvcController>()
                .CallingAsync(c => c.AsyncOkResultAction());

            this.CheckActionResultTestBuilder(actionResultTestBuilder, "AsyncOkResultAction");
        }

        [Fact]
        public void CallingShouldPopulateCorrectActionNameWithNormalVoidActionCall()
        {
            var voidActionResultTestBuilder = MyMvc
                .Controller<MvcController>()
                .Calling(c => c.EmptyAction());

            this.CheckActionName(voidActionResultTestBuilder, "EmptyAction");
        }

        [Fact]
        public void CallingShouldPopulateCorrectActionNameWithTaskActionCall()
        {
            var voidActionResultTestBuilder = MyMvc
                .Controller<MvcController>()
                .CallingAsync(c => c.EmptyActionAsync());

            this.CheckActionName(voidActionResultTestBuilder, "EmptyActionAsync");
        }

        [Fact]
        public void CallingShouldPopulateModelStateWhenThereAreModelErrors()
        {
            var requestModel = TestObjectFactory.GetRequestModelWithErrors();

            var controller = MyMvc
                .Controller<MvcController>()
                .Calling(c => c.OkResultActionWithRequestBody(1, requestModel))
                .ShouldReturn()
                .Ok()
                .AndProvideTheController();

            var modelState = controller.ModelState;

            Assert.False(modelState.IsValid);
            Assert.Equal(2, modelState.Values.Count);
            Assert.Equal("Integer", modelState.Keys.First());
            Assert.Equal("RequiredString", modelState.Keys.Last());
        }

        [Fact]
        public void CallingShouldHaveValidModelStateWhenThereAreNoModelErrors()
        {
            var requestModel = TestObjectFactory.GetValidRequestModel();

            var controller = MyMvc
                .Controller<MvcController>()
                .Calling(c => c.OkResultActionWithRequestBody(1, requestModel))
                .ShouldReturn()
                .Ok()
                .AndProvideTheController();

            var modelState = controller.ModelState;

            Assert.True(modelState.IsValid);
            Assert.Equal(0, modelState.Values.Count);
            Assert.Equal(0, modelState.Keys.Count);
        }

        [Fact]
        public void WithoutValidationShouldNotValidateTheRequestModel()
        {
            MyMvc
                .Controller<MvcController>()
                .WithoutValidation()
                .Calling(c => c.ModelStateCheck(TestObjectFactory.GetRequestModelWithErrors()))
                .ShouldHave()
                .ValidModelState();
        }
        
        [Fact]
        public void WithAuthenticatedUserShouldPopulateUserPropertyWithDefaultValues()
        {
            var controllerBuilder = MyMvc
                .Controller<MvcController>()
                .WithAuthenticatedUser();

            controllerBuilder
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();

            var controllerUser = controllerBuilder.AndProvideTheController().User;

            Assert.Equal(false, controllerUser.IsInRole("Any"));
            Assert.Equal("TestUser", controllerUser.Identity.Name);
            Assert.True(controllerUser.HasClaim(ClaimTypes.Name, "TestUser"));
            Assert.Equal("Passport", controllerUser.Identity.AuthenticationType);
            Assert.Equal(true, controllerUser.Identity.IsAuthenticated);
        }

        [Fact]
        public void WithAuthenticatedUserShouldPopulateProperUserWhenUserWithUserBuilder()
        {
            var controllerBuilder = MyMvc
                .Controller<MvcController>()
                .WithAuthenticatedUser(user => user
                    .WithUsername("NewUserName")
                    .WithAuthenticationType("Custom")
                    .InRole("NormalUser")
                    .AndAlso()
                    .InRoles("Moderator", "Administrator")
                    .InRoles(new[]
                    {
                        "SuperUser",
                        "MegaUser"
                    }));

            controllerBuilder
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();

            var controllerUser = controllerBuilder.AndProvideTheController().User;

            Assert.Equal("NewUserName", controllerUser.Identity.Name);
            Assert.Equal("Custom", controllerUser.Identity.AuthenticationType);
            Assert.Equal(true, controllerUser.Identity.IsAuthenticated);
            Assert.Equal(true, controllerUser.IsInRole("NormalUser"));
            Assert.Equal(true, controllerUser.IsInRole("Moderator"));
            Assert.Equal(true, controllerUser.IsInRole("Administrator"));
            Assert.Equal(true, controllerUser.IsInRole("SuperUser"));
            Assert.Equal(true, controllerUser.IsInRole("MegaUser"));
            Assert.Equal(false, controllerUser.IsInRole("AnotherRole"));
        }

        [Fact]
        public void WithAuthenticatedNotCalledShouldNotHaveAuthorizedUser()
        {
            var controllerBuilder = MyMvc
                .Controller<MvcController>();

            controllerBuilder
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .HttpNotFound();

            var controllerUser = controllerBuilder.AndProvideTheController().User;

            Assert.Equal(false, controllerUser.IsInRole("Any"));
            Assert.Equal(null, controllerUser.Identity.Name);
            Assert.Equal(null, controllerUser.Identity.AuthenticationType);
            Assert.Equal(false, controllerUser.Identity.IsAuthenticated);
        }
        
        [Fact]
        public void WithResolvedDependencyForShouldChooseCorrectConstructorWithLessDependencies()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencyFor<IInjectedService>(new InjectedService())
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.InjectedService);
            Assert.Null(controller.AnotherInjectedService);
            Assert.Null(controller.InjectedRequestModel);
        }

        [Fact]
        public void WithResolvedDependencyForShouldChooseCorrectConstructorWithMoreDependencies()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencyFor<IAnotherInjectedService>(new AnotherInjectedService())
                .WithResolvedDependencyFor<IInjectedService>(new InjectedService())
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.InjectedService);
            Assert.NotNull(controller.AnotherInjectedService);
            Assert.Null(controller.InjectedRequestModel);
        }

        [Fact]
        public void WithResolvedDependencyForShouldChooseCorrectConstructorWithAllDependencies()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencyFor<IAnotherInjectedService>(new AnotherInjectedService())
                .WithResolvedDependencyFor<RequestModel>(new RequestModel())
                .WithResolvedDependencyFor<IInjectedService>(new InjectedService())
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.InjectedService);
            Assert.NotNull(controller.AnotherInjectedService);
            Assert.NotNull(controller.InjectedRequestModel);
        }

        [Fact]
        public void WithResolvedDependencyForShouldContinueTheNormalExecutionFlowOfTestBuilders()
        {
            MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencyFor(new RequestModel())
                .WithResolvedDependencyFor(new AnotherInjectedService())
                .WithResolvedDependencyFor(new InjectedService())
                .WithAuthenticatedUser()
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();
        }

        [Fact]
        public void AndAlsoShouldContinueTheNormalExecutionFlowOfTestBuilders()
        {
            MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencyFor(new RequestModel())
                .AndAlso()
                .WithResolvedDependencyFor(new AnotherInjectedService())
                .AndAlso()
                .WithResolvedDependencyFor(new InjectedService())
                .AndAlso()
                .WithAuthenticatedUser()
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();
        }

        [Fact]
        public void WithResolvedDependenciesShouldWorkCorrectWithCollectionOfObjects()
        {
            MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencies(new List<object> { new RequestModel(), new AnotherInjectedService(), new InjectedService() })
                .WithAuthenticatedUser()
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();
        }

        [Fact]
        public void WithResolvedDependenciesShouldWorkCorrectWithParamsOfObjects()
        {
            MyMvc
                .Controller<MvcController>()
                .WithResolvedDependencies(new RequestModel(), new AnotherInjectedService(), new InjectedService())
                .WithAuthenticatedUser()
                .Calling(c => c.AuthorizedAction())
                .ShouldReturn()
                .Ok();
        }

        [Fact]
        public void WithResolvedDependencyForShouldThrowExceptionWhenSameDependenciesAreRegistered()
        {
            Test.AssertException<InvalidOperationException>(
                () =>
                {
                    MyMvc
                        .Controller<MvcController>()
                        .WithResolvedDependencyFor<RequestModel>(new RequestModel())
                        .WithResolvedDependencyFor<IAnotherInjectedService>(new AnotherInjectedService())
                        .WithResolvedDependencyFor<IInjectedService>(new InjectedService())
                        .WithResolvedDependencyFor<IAnotherInjectedService>(new AnotherInjectedService());
                },
                "Dependency AnotherInjectedService is already registered for MvcController controller.");
        }

        [Fact]
        public void WithResolvedDependencyForShouldThrowExceptionWhenNoConstructorExistsForDependencies()
        {
            Test.AssertException<UnresolvedDependenciesException>(
                () =>
                {
                    MyMvc
                        .Controller<MvcController>()
                        .WithResolvedDependencyFor<RequestModel>(new RequestModel())
                        .WithResolvedDependencyFor<IAnotherInjectedService>(new AnotherInjectedService())
                        .WithResolvedDependencyFor<IInjectedService>(new InjectedService())
                        .WithResolvedDependencyFor<ResponseModel>(new ResponseModel())
                        .Calling(c => c.OkResultAction())
                        .ShouldReturn()
                        .Ok();
                }, 
                "MvcController could not be instantiated because it contains no constructor taking RequestModel, AnotherInjectedService, InjectedService, ResponseModel as parameters.");
        }

        [Fact]
        public void PrepareControllerShouldSetCorrectPropertiesWithDefaultServices()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.HttpContext);
            Assert.NotNull(controller.ControllerContext);
            Assert.NotNull(controller.ControllerContext.HttpContext);
            Assert.NotNull(controller.ControllerContext.InputFormatters);
            Assert.NotEmpty(controller.ControllerContext.InputFormatters);
            Assert.NotNull(controller.ControllerContext.ModelBinders);
            Assert.NotEmpty(controller.ControllerContext.ModelBinders);
            Assert.NotNull(controller.ControllerContext.ValidatorProviders);
            Assert.NotEmpty(controller.ControllerContext.ValidatorProviders);
            Assert.NotNull(controller.ControllerContext.ValueProviders);
            Assert.NotNull(controller.ViewBag);
            Assert.NotNull(controller.ViewData);
            Assert.NotNull(controller.TempData);
            Assert.NotNull(controller.Resolver);
            Assert.NotNull(controller.Request);
            Assert.NotNull(controller.Response);
            Assert.NotNull(controller.MetadataProvider);
            Assert.NotNull(controller.ModelState);
            Assert.NotNull(controller.ObjectValidator);
            Assert.NotNull(controller.Url);
            Assert.NotNull(controller.User);
            Assert.Null(controller.ControllerContext.ActionDescriptor);
        }

        [Fact]
        public void PrepareControllerShouldSetCorrectPropertiesWithCustomSetup()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .WithSetup(c =>
                {
                    c.ControllerContext = new ControllerContext();
                })
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.ControllerContext);
            Assert.Null(controller.ControllerContext.HttpContext);
            Assert.Empty(controller.ControllerContext.InputFormatters);
            Assert.Empty(controller.ControllerContext.ModelBinders);
            Assert.Empty(controller.ControllerContext.ValidatorProviders);
        }

        [Fact]
        public void CallingShouldPopulateCorrectActionDescriptor()
        {
            var controller = MyMvc
                .Controller<MvcController>()
                .Calling(c => c.OkResultAction())
                .AndProvideTheController();

            Assert.NotNull(controller);
            Assert.NotNull(controller.ControllerContext);
            Assert.NotNull(controller.ControllerContext.ActionDescriptor);
            Assert.Equal("OkResultAction", controller.ControllerContext.ActionDescriptor.Name);
        }
        
        private void CheckActionResultTestBuilder<TActionResult>(
            IActionResultTestBuilder<TActionResult> actionResultTestBuilder,
            string expectedActionName)
        {
            this.CheckActionName(actionResultTestBuilder, expectedActionName);
            var actionResult = actionResultTestBuilder.AndProvideTheActionResult();

            Assert.NotNull(actionResult);
            Assert.IsAssignableFrom<HttpOkResult>(actionResult);
        }

        private void CheckActionName(IBaseTestBuilderWithCaughtException testBuilder, string expectedActionName)
        {
            var actionName = testBuilder.AndProvideTheActionName();

            Assert.NotNull(actionName);
            Assert.NotEmpty(actionName);
            Assert.Equal(expectedActionName, actionName);
        }
    }
}