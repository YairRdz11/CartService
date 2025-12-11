using System.Text.Json;
using CartService.API.Infrastructure.RabbitMq;
using CartService.BLL;
using CartService.Transversal.Classes.Messages;
using CartService.Transversal.Classes.Models.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CartService.Transversal.Interfaces.DAL;

namespace CartService.Testing.UnitTesting
{
 public class ProductEventFacadeTests
 {
 private static ProductEventFacade CreateFacade(Mock<ICartRepository> repoMock, Mock<ILogger<ProductEventFacade>> loggerMock)
 {
 var updateService = new ProductUpdateService(repoMock.Object);
 return new ProductEventFacade(updateService, loggerMock.Object);
 }

 [Fact]
 public async Task ProcessAsync_MissingEventType_ReturnsError()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var json = JsonSerializer.Serialize(new { /* no eventType */ });
 var result = await facade.ProcessAsync(json, default);
 Assert.False(result.Success);
 Assert.Equal("Missing eventType", result.Error);
 Assert.Null(result.EventType);
 }

 [Fact]
 public async Task ProcessAsync_UnhandledEvent_ReturnsError()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var json = JsonSerializer.Serialize(new { eventType = "Unknown" });
 var result = await facade.ProcessAsync(json, default);
 Assert.False(result.Success);
 Assert.Equal("Unhandled eventType", result.Error);
 Assert.Equal("Unknown", result.EventType);
 }

 [Fact]
 public async Task ProductDeleted_WithoutProductId_ReturnsError()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var json = JsonSerializer.Serialize(new { eventType = "ProductDeletedEvent" });
 var result = await facade.ProcessAsync(json, default);
 Assert.False(result.Success);
 Assert.Equal("productId missing", result.Error);
 Assert.Equal("ProductDeletedEvent", result.EventType);
 }

 [Fact]
 public async Task ProductDeleted_WithProductId_InvokesUpdateService_ReturnsAffected()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var pid = Guid.NewGuid();
 repo.Setup(r => r.RemoveProduct(pid)).Returns(3);
 var json = JsonSerializer.Serialize(new { eventType = "ProductDeletedEvent", productId = pid });
 var result = await facade.ProcessAsync(json, default);
 Assert.True(result.Success);
 Assert.Equal("ProductDeletedEvent", result.EventType);
 Assert.Equal(3, result.AffectedCarts);
 repo.Verify(r => r.RemoveProduct(pid), Times.Once);
 }

 [Fact]
 public async Task ProductUpdated_PayloadWithoutFields_DeserializesDefaults_InvokesUpdateService()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 // Only eventType present; message fields will be default/null
 var json = JsonSerializer.Serialize(new { eventType = "ProductUpdatedEvent" });
 repo.Setup(r => r.UpdateProductInfo(default, null, null, null)).Returns(0);
 var result = await facade.ProcessAsync(json, default);
 Assert.True(result.Success);
 Assert.Equal("ProductUpdatedEvent", result.EventType);
 Assert.Equal(0, result.AffectedCarts);
 repo.Verify(r => r.UpdateProductInfo(default, null, null, null), Times.Once);
 }

 [Fact]
 public async Task ProductUpdated_ValidPayload_InvokesUpdateService()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var pid = Guid.NewGuid();
 var catId = Guid.NewGuid();
 var combined = JsonSerializer.Serialize(new { eventType = "ProductUpdatedEvent", productId = pid, name = "NewName", price =9.99m, categoryId = catId });
 repo.Setup(r => r.UpdateProductInfo(pid, "NewName",9.99m, catId)).Returns(2);
 var result = await facade.ProcessAsync(combined, default);
 Assert.True(result.Success);
 Assert.Equal("ProductUpdatedEvent", result.EventType);
 Assert.Equal(2, result.AffectedCarts);
 repo.Verify(r => r.UpdateProductInfo(pid, "NewName",9.99m, catId), Times.Once);
 }

 [Fact]
 public async Task CategoryUpdated_ValidPayload_ReturnsSuccess()
 {
 var repo = new Mock<ICartRepository>();
 var logger = new Mock<ILogger<ProductEventFacade>>();
 var facade = CreateFacade(repo, logger);
 var json = JsonSerializer.Serialize(new { eventType = "CategoryUpdatedEvent", categoryId = Guid.NewGuid(), name = "Cat" });
 var result = await facade.ProcessAsync(json, default);
 Assert.True(result.Success);
 Assert.Equal("CategoryUpdatedEvent", result.EventType);
 Assert.Equal(0, result.AffectedCarts);
 }
 }
}
