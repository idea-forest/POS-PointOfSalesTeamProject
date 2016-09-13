﻿using POS_DataLibrary;
using POS_PointOfSales.ViewModels;
using POS_ViewsLibrary;
using System.ComponentModel;
using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Drawing.Printing;
using System.Drawing;
using POS_SellersApp.Views;
using System.Windows.Input;

namespace POS_SellersApp.ViewModels
{

    public class SellersMainWindowViewModel : ViewModel

    {
        private const double TAX = 0.15;
        private bool IsDoneMessageReceived;
        private Database db;
        private ObservableCollection<Int32> discountButtons;
        public ObservableCollection<Int32> DiscountButtons
        {
            get
            {
                return discountButtons;
            }

            set
            {
                discountButtons = value;
                RaisePropertyChanged("DiscountButtons");
            }
        }
        //   public User UserLoggedIn = new User();
        public SellersMainWindowViewModel()
        {

            db = new Database();
            //Register for messages from differnet viewModels
           
            MessengerPoduct.Default.Register<Product>(this, (product) =>
            {
                ReceiveMessage(product);
            });
            if (!IsDoneMessageReceived)
            {
                MessengerDone.Default.Register<String>(this, message =>
                   {
                       IsDoneMessageReceived = true;
                       if (!IsDoneMessageReceived)
                       {
                           RecivedDoneMessage(message);
                       }
                       
                   }
                );
            }
            SendLogoutMessage = new ActionCommand(p => OnSendLogoutMessage("login"));
            //Initialize comopents with some values
            OrderItems = new ObservableCollection<OrderItems>();
            DiscountButtons = new ObservableCollection<Int32>();
            for (int i = 0; i <= 20; i = i + 5)
            {
                DiscountButtons.Add(i);
            }
            DiscountButtons.Add(50);
            DiscountButtons.Add(100);
            SelectedDiscount = 0;
            
            //Register CollectionChangedEvent for the OrderItems
            OrderItems.CollectionChanged += ContentCollectionChanged;
            //if (OrderItems != null && OrderItems.CanGroup == true)
            //{
            //    OrderItems.GroupDescriptions.Clear();
            //    OrderItems.GroupDescriptions.Add(new PropertyGroupDescription("ProjectName"));

                //Initialize commands
                RemoveItem = new ActionCommand(p => OnRemoveItem());
            CancelOrder = new ActionCommand(p => OnCancelOrder());
            SwitchViews = new ActionCommand((p) => OnSwitchViews(p.ToString()));
            PrintReceipt = new ActionCommand(p => OnPrintReceipt());
            currentView = ProductsCatalogViewModel;
            OrderNo = "1";
            
            //Set the totals to zero
            //  OrderSubTotal = 0;
        }



        private void RecivedDoneMessage(string message)
        {
            if (IsDoneMessageReceived) return;
            switch (message)
            {
                case "Register":
                   
                    Order order = new Order { Date = DateTime.Now, Discount = SelectedDiscount/100, StoreNo = "OV001", UserId = "SEL02", OrderAmount = BalanceDue, Tax = OrderTax };

                    try
                    {
                        if (OrderItems.Count > 0)
                        {
                            int lastOrderId = db.saveOrderAndOrderItems(order, OrderItems.ToList());
                            OrderNo = string.Format("Order# {0}", lastOrderId.ToString());
                            CurrentView = ProductsCatalogViewModel;
                            OrderItems.Clear();
                        }
                        MessageBox.Show("Order has been sent to processing");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error inserting data to the database");
                        throw ex;
                    }
                    break;
                case "Back":
                default:
                    OnSwitchViews("catalog");
                    break;
            }
           

        }

        public void ContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("OrderSubTotal");
            RaisePropertyChanged("OrderTax");
            RaisePropertyChanged("SelectedDiscount");
            RaisePropertyChanged("BalanceDue");
            RaisePropertyChanged("SelectedOrderItem");
        }
        private ObservableCollection<OrderItems> orderItems;

        public ObservableCollection<OrderItems> OrderItems
        {
            get { return orderItems; }
            set
            {
                orderItems = value;
                RaisePropertyChanged("OrderItems");
            }
        }

        private void ReceiveMessage(Product product)
        {
            //Check if the product was already selected
            if (OrderItems.Any(p => p.UPCCode == product.UPCCode))
            {
                OrderItems.First(p => p.UPCCode == product.UPCCode).Quantity += 1;
            }
            else
            {
                OrderItems.Add(new POS_DataLibrary.OrderItems
                {
                    UPCCode = product.UPCCode,
                    CategoryName = product.CategoryName.ToString(),
                    Quantity = 1,
                    Price = product.Price,
                    Name = product.Name,
                });
            }
        }
        private Int32 selectedDiscount;
        public Int32 SelectedDiscount {
            get
            {
                return selectedDiscount;
            }

            set
            {
                selectedDiscount = value;
                RaisePropertyChanged("SelectedDiscount");
                RaisePropertyChanged("Discount");
                RaisePropertyChanged("OrderTax");
                RaisePropertyChanged("BalanceDue");
            }
        }
        public decimal Discount
        {
            get
            {
                return (OrderSubTotal* SelectedDiscount / 100);
            }
        }
        private OrderItems selectedOrderItem;
        public OrderItems SelectedOrderItem
        {
            get
            {
                return selectedOrderItem;
            }

            set
            {
                selectedOrderItem = value;
                RaisePropertyChanged("SelectedOrderItem");
            }
        }

        private User userLoggedIn;
        public User UserLoggedIn
        {
            get
            {
                return userLoggedIn;
            }

            set

            {
                userLoggedIn = value;

                RaisePropertyChanged("UserLoggedIn");
                RaisePropertyChanged("FirstName");
            }
        }


        public string FirstName
        {
            get
            {
                if (UserLoggedIn != null)
                {
                    return UserLoggedIn.FirstName;
                }
                return "NOT SET";
            }
        }
        public decimal OrderSubTotal
        {
            get
            {
                decimal value = orderItems.Sum(od => od.Total);
                if (value == 0)
                {
                    return 0;
                }
                else
                {
                    return value;
                }

            }
        }
        public decimal OrderTax
        {
            get
            {

                return (OrderSubTotal - Discount) *(decimal)TAX;
            }

        }
        public decimal BalanceDue
        {
            get
            {
                return (OrderSubTotal - Discount + OrderTax);
            }

        }
        private string orderNo;
        public string OrderNo
        {
            get
            {
                return orderNo;
            }

            set
            {
                orderNo = value;
                RaisePropertyChanged("OrderNo");
            }
        }

        private ProductsCatalogViewModel ProductsCatalogViewModel = new ProductsCatalogViewModel();

        //    private PaimentViewModel payiementViewModel = new PaimentViewModel();


        private ViewModel currentView;

        public ViewModel CurrentView
        {
            get { return currentView; }
            set {
                currentView = value;
                RaisePropertyChanged("CurrentView"); }
        }
        public string DisplayedImagePath
        {
            get { return "/POS-PointOfSales;component/Logo_Small.png"; }
        }
        #region Commands
        private void OnSendLogoutMessage(string v)
        {
            MessengerLogout.Default.Send(v);
        }

        public ActionCommand SendLogoutMessage { get; private set; }
        public ActionCommand RemoveItem { get; private set; }
        public ActionCommand CancelOrder { get; private set; }

        private void OnRemoveItem()
        {
            if (SelectedOrderItem != null)
            {
                MessageBoxResult result = MessageBox.Show("You are about to delete an item. Do you want to continue?", "Delete Item", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    OrderItems.Remove(SelectedOrderItem);
                };
            }
        }
        private void OnCancelOrder()
        {
            if (OrderItems.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("You are about to delete current order. Do you want to continue?", "Delete Order", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    OrderItems.Clear();
                };
            }
        }
        public ActionCommand SwitchViews { get; private set; }
       readonly static PaimentViewModel pvm = new PaimentViewModel();
        private void OnSwitchViews(string destination)
        {
            switch (destination)
            {
                case "pay":
                    pvm.Balance = BalanceDue.ToString("#.##");
                    CurrentView = pvm;
                    MessengerBalance.Default.Send(BalanceDue);
                    break;

                case "catalog":
                default:
                    CurrentView = ProductsCatalogViewModel;
                    break;
            }

        }
        #endregion
        //public void OpenChildDailog()
        //{
        //    DialogService service = new DialogService();
        //    paiementVM.Balance = BalanceDue.ToString(); // Assign whatever you want
        //    service.Show(new PaymentView(), paiementVM);

        //    // Now get the values when the child dailog get closed

        //    var retVal = paiementVM.Change;

        //}

        #region PrintReceipt
        public ActionCommand PrintReceipt { get; private set; }
       
       
        private void OnPrintReceipt()
        {
            var doc = new PrintDocument();
            doc.PrintPage += new PrintPageEventHandler(CreateReceipt);
            doc.Print();
        }

        public void CreateReceipt(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {

            //this prints the reciept

            Graphics graphic = e.Graphics;
            Font font = new Font("Courier New", 12); //must use a mono spaced font as the spaces need to line up

            float fontHeight = font.GetHeight();

            int startX = 10;
            int startY = 10;
            int offset = 40;

            graphic.DrawString(" Olya&Valya", new Font("Courier New", 18), new SolidBrush(Color.Black), startX, startY);
            string top = "Item Name".PadRight(30) + "Price";
            graphic.DrawString(top, font, new SolidBrush(Color.Black), startX, startY + offset);
            offset = offset + (int)fontHeight; //make the spacing consistent
            graphic.DrawString("----------------------------------", font, new SolidBrush(Color.Black), startX, startY + offset);
            offset = offset + (int)fontHeight + 5; //make the spacing consistent

            decimal totalprice = 0;

            foreach (var item in orderItems)
            {
                //create the string to print on the reciept
                string productDescription = item.Name;
                string productQty = item.Quantity.ToString();
                string productPrice = item.Price.ToString();
                string productTotal = item.Total.ToString();

                string productLine = item.CategoryName;

                graphic.DrawString(productLine, font, new SolidBrush(Color.Black), startX, startY + offset);

                offset = offset + (int)fontHeight + 5; //make the spacing consistent
            }
            //TODO: Pass the paiement information
            decimal change = 0;
            decimal totalPrice = 0;
            decimal cash = 0;
            change = (cash - totalprice);

            //when we have drawn all of the items add the total

            offset = offset + 20; //make some room so that the total stands out.

            graphic.DrawString("Total to pay ".PadRight(30) + String.Format("{0:c}", totalprice), new Font("Courier New", 12, System.Drawing.FontStyle.Bold), new SolidBrush(Color.Black), startX, startY + offset);

            offset = offset + 30; //make some room so that the total stands out.
            graphic.DrawString("CASH ".PadRight(30) + String.Format("{0:c}", cash), font, new SolidBrush(Color.Black), startX, startY + offset);
            offset = offset + 15;
            graphic.DrawString("CHANGE ".PadRight(30) + String.Format("{0:c}", change), font, new SolidBrush(Color.Black), startX, startY + offset);
            offset = offset + 30; //make some room so that the total stands out.
            graphic.DrawString("     Thank-you for your custom,", font, new SolidBrush(Color.Black), startX, startY + offset);
            offset = offset + 15;
            graphic.DrawString("       please come back soon!", font, new SolidBrush(Color.Black), startX, startY + offset);

        }
        #endregion
        private ICommand _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand == null)
                    _closeCommand = new ActionCommand(param => this.OnRequestClose());

                return _closeCommand;
            }
        }

        public event EventHandler RequestClose;

        void OnRequestClose()
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }


    }

}