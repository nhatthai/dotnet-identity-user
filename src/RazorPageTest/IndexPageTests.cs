﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IdentityUsers.Hubs;
using IdentityUsers.Pages.Account;
using IdentityUsers.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace RazorPageTest
{
    public class IndexPageTests
    {

        [Fact]
        public void OnGet_HomePageModel()
        {
            var pageModel = new IdentityUsers.Pages.IndexModel();
            pageModel.OnGet();
        }

        [Fact]
        public async Task OnPostAsync_ReturnAsPageResult()
        {
            var hub = new Mock<IHubContext<NotificationUserHub>>();
            var userManager = new Mock<IUserConnectionManager>();
            var pageModel = new MessageModel(hub.Object, userManager.Object);

            // Act
            var result = await pageModel.OnPostAsync("Hello");

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnGetAsync_ReturnAsPageResult()
        {
            // create user context
            HttpContext context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Name, "Test user"),
                new Claim(ClaimTypes.Email, "test@email.com"),
                new Claim(ClaimTypes.Role, "Admin")
            }));

            var mockUserStore = new Mock<IUserStore<IdentityUser>>();

            // Arrange
            mockUserStore.Setup(x => x.FindByIdAsync("123", CancellationToken.None))
                .ReturnsAsync(new IdentityUser
                {
                    UserName = "test@email.com",
                    Id = "123"
                });
           
            // Arrange.
            var users = new List<IdentityUser>
            {
                new IdentityUser { Id="123", UserName = "test@email.com" },
                new IdentityUser { Id="124", UserName = "user@test.com" }
            }.AsQueryable();
            var mockUserManager = new Mock<UserManager<IdentityUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);
            mockUserManager.Setup(x => x.Users).Returns(users);
            mockUserManager.Setup(x => x.GetUserAsync(context.User))
                .Returns(Task.FromResult(new IdentityUser { Id = "123", UserName = "test@email.com" }));

            // https://docs.microsoft.com/en-us/aspnet/core/test/razor-pages-tests?view=aspnetcore-2.1#unit-tests-of-the-page-model-methods
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(context, new RouteData(), new PageActionDescriptor(), modelState);
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);

            // need page context for the page model
            var pageContext = new PageContext(actionContext)
            {
                ViewData = viewData
            };

            //set context into Room Page
            var pageModel = new RoomModel(mockUserManager.Object) {
                PageContext = pageContext
            };

            // Act
            var result = await pageModel.OnGetAsync();

            // Assert
            Assert.IsType<PageResult>(result);
        }
    }
}