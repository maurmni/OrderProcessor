using Xunit;
using Moq;
using OrderProcessor;
using System;

namespace OrderProcessorTest
{
    public class OrderProcessorTests
    {
        private readonly Mock<IDatabase> _mockDatabase;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly OrderProcesssor _orderProcessor;

        public OrderProcessorTests()
        {
            _mockDatabase = new Mock<IDatabase>();
            _mockEmailService = new Mock<IEmailService>();
            _orderProcessor = new OrderProcesssor(_mockDatabase.Object, _mockEmailService.Object);
        }

        //happy path 
        [Fact]
        public void ProcessOrder_WithValidOrderAndAmountOver100()
        {
            //arrange
            var order = new Order { Id = 1, CustomerEmail = "test@test.com", TotalAmount = 150 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            Assert.True(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation("test@test.com", 1), Times.Once);
        }

        [Fact]
        public void ProcessOrder_WithValidOrderAndAmountExactly100()
        {
            //arrange
            var order = new Order { Id = 2, CustomerEmail = "test@test.com", TotalAmount = 100 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            Assert.True(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_WhenDatabaseNotConnected()
        {
            //arrange
            var order = new Order { Id = 3, CustomerEmail = "test@test.com", TotalAmount = 50 };
            _mockDatabase.SetupSequence(db => db.IsConnected)
                .Returns(false)
                .Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            _mockDatabase.Verify(db => db.Connect(), Times.Once);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
        }

        //граничные случаи и ошибочные сценарии
        [Fact]
        public void ProcessOrder_WithNullOrder()
        {
            //arrange
            Order order = null;

            //act & assert
            Assert.Throws<ArgumentNullException>(() => _orderProcessor.ProcessOrder(order));
        }

        [Fact]
        public void ProcessOrder_WithZeroTotalAmount()
        {
            //arrange
            var order = new Order { Id = 4, CustomerEmail = "test@test.com", TotalAmount = 0 };

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.False(result);
            Assert.False(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(It.IsAny<Order>()), Times.Never);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_WithNegativeTotalAmount()
        {
            //arrange
            var order = new Order { Id = 5, CustomerEmail = "test@test.com", TotalAmount = -50 };

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.False(result);
            Assert.False(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(It.IsAny<Order>()), Times.Never);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_WhenDatabaseSaveThrowsException()
        {
            //arrange
            var order = new Order { Id = 6, CustomerEmail = "test@test.com", TotalAmount = 200 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);
            _mockDatabase.Setup(db => db.Save(order)).Throws(new Exception("Database error"));

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.False(result);
            Assert.False(order.IsProcessed);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_WithAmountEqualTo101()
        {
            //arrange
            var order = new Order { Id = 7, CustomerEmail = "test@test.com", TotalAmount = 101 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            _mockEmailService.Verify(es => es.SendOrderConfirmation("test@test.com", 7), Times.Once);
        }

        [Fact]
        public void ProcessOrder_WithAmountEqualTo99()
        {
            //arrange
            var order = new Order { Id = 8, CustomerEmail = "test@test.com", TotalAmount = 99 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            _mockEmailService.Verify(es => es.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_WithEmptyCustomerEmail()
        {
            //arrange
            var order = new Order { Id = 9, CustomerEmail = "", TotalAmount = 150 };
            _mockDatabase.Setup(db => db.IsConnected).Returns(true);

            //act
            var result = _orderProcessor.ProcessOrder(order);

            //assert
            Assert.True(result);
            Assert.True(order.IsProcessed);
            _mockDatabase.Verify(db => db.Save(order), Times.Once);
            _mockEmailService.Verify(es => es.SendOrderConfirmation("", 9), Times.Once);
        }
    }
}