using System;

namespace OrderProcessor
{
    public interface IDatabase
    {
        bool IsConnected { get; }
        void Connect();
        void Save(Order order);
        Order GetOrder(int id);
    }

    public interface IEmailService
    {
        void SendOrderConfirmation(string customerEmail, int orderId);
    }

    public class Order
    {
        public int Id { get; set; }
        public string CustomerEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsProcessed { get; set; }
    }
    public class OrderProcesssor
    {
        private readonly IDatabase _database;
        private readonly IEmailService _emailService;
        private const decimal  emailThreashhold= 100m;

        public OrderProcesssor(IDatabase database, IEmailService emailService)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public bool ProcessOrder(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.TotalAmount <= 0) return false;

            try
            {
                EnsureDatabaseConnection();
                _database.Save(order);

                if (order.TotalAmount > emailThreashhold)
                {
                    _emailService.SendOrderConfirmation(order.CustomerEmail, order.Id);
                }

                order.IsProcessed = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void EnsureDatabaseConnection()
        {
            if (!_database.IsConnected)
            {
                _database.Connect();
            }
        }
    }
}