using MyNorthwindLinqQueries.DbFirst;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MyNorthwindLinqQueries
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (NorthwindEntities db = new NorthwindEntities())
            {
                // hangi calisan toplam kaç adet satıs yap, hangi ülkeye satıs yapmıs

                // string countries = string.Join(",", db.Customers.Select(a => a.Country).ToList());

                //The basic gist of it is that you cannot access string.join in EF queries. You must create the LINQ query, then call ToList() in order to execute the query against the db. Then you have the data in memory(aka LINQ to Objects), so you can access string.join.
                //(gr.Select(x=>x.c.Country)).Aggregate((a,b)=>(a+", "+b))

                var result1 = (from c in db.Customers
                               join od in db.Orders on c.CustomerID equals od.CustomerID
                               join emp in db.Employees on od.EmployeeID equals emp.EmployeeID
                               group new { emp, c, od } by new { emp.FirstName } into gr
                               let countries = (from cx in gr.Select(a => a.c)
                                                join o in gr.Select(a => a.od) on cx.CustomerID equals o.CustomerID
                                                join em in gr.Select(a => a.emp) on o.EmployeeID equals em.EmployeeID
                                                select new { cx.Country }).ToList()
                               select new
                               {
                                   EmployeeName = gr.Key.FirstName,
                                   CompanyName = gr.Select(x => x.c.CompanyName).FirstOrDefault(),
                                   SalesCount = gr.Count(x => x.od.OrderID > 0),
                                   CountryName = countries.AsEnumerable().Select(x => x.Country).Distinct()
                               }).AsEnumerable().Select(x => new
                               {
                                   EmployeeName = x.EmployeeName,
                                   CompanyName = x.CompanyName,
                                   SalesCount = x.SalesCount,
                                   CountryName = string.Join(",", x.CountryName)
                               }).ToList();

                //dgv.DataSource = result1;




                ////1.Give the name, address, city, and region of employees.

                var query1 = (from emp in db.Employees
                              select new { emp.FirstName, emp.LastName, emp.Address, emp.City, emp.Region }).ToList();

                ////2.Give the name, address, city, and region of employees living in USA

                var query2 = (from emp in db.Employees
                              where emp.Region == "USA"
                              select new { emp.FirstName, emp.LastName, emp.Address, emp.City, emp.Region }).ToList();

                ////3.Give the name, address, city, and region of employees older than 50 years old

                var query3 = (from emp in db.Employees
                              where DbFunctions.DiffYears(emp.BirthDate, DateTime.Today) > 50
                              select new
                              {
                                  emp.FirstName,
                                  emp.LastName,
                                  emp.Address,
                                  emp.City,
                                  emp.Region,
                                  Age = DbFunctions.DiffYears(emp.BirthDate, DateTime.Today)
                              }).ToList();

                ////dgv.DataSource = query3;

                ////4.Give the name, address, city, and region of employees that have placed orders to be
                ////delivered in Belgium.Write two versions of the query, with and without join.

                //// gruplamadan yaparsak employe duplicate oluyor! ya da distinct() kullanmalıydık!
                var query4 = (from emp in db.Employees
                              join ord in db.Orders on emp.EmployeeID equals ord.EmployeeID
                              where ord.ShipCountry == "Belgium"
                              group new { emp, ord } by new { emp.FirstName, emp.LastName, emp.Region, ord.ShipCountry } into g
                              select new { g.Key.FirstName, g.Key.LastName, g.Key.Region, g.Key.ShipCountry }).ToList();


                ////dgv.DataSource = query4;

                ////5.Give the employee name and the customer name for orders that are sent by the
                ////company ‘Speedy Express’ to customers who live in Brussels. (Bruxelles)

                var query5 = (from emp in db.Employees
                              join ord in db.Orders on emp.EmployeeID equals ord.EmployeeID
                              join shp in db.Shippers on ord.ShipVia equals shp.ShipperID
                              join c in db.Customers on ord.CustomerID equals c.CustomerID
                              where c.City == "Bruxelles" && shp.CompanyName == "Speedy Express"
                              let empName = emp.FirstName + " " + emp.LastName
                              select new { empName, c.ContactName, c.CompanyName, c.City }).ToList();

                ////dgv.DataSource = query5;


                ////6.Give the title and name of employees who have sold at least one of the products
                ////‘Gravad Lax’ or ‘Mishi Kobe Niku’.

                var query6 = (from emp in db.Employees
                              join ord in db.Orders on emp.EmployeeID equals ord.EmployeeID
                              join od in db.Order_Details on ord.OrderID equals od.OrderID
                              join p in db.Products on od.ProductID equals p.ProductID
                              where p.ProductName == "Gravad Lax" || p.ProductName == "Mishi Kobe Niku"
                              select new { emp.Title, emp.FirstName, emp.LastName }).Distinct().ToList();

                ////dgv.DataSource = query6;


                ////7.Give the name and title of employees and the name and title of the person to which
                ////they refer(or null for the latter values if they don’t refer to another employee).

                var query7 = (from emp in db.Employees
                              join m1 in db.Employees on emp.ReportsTo equals m1.EmployeeID
                              let manager = m1.Title + " " + m1.FirstName + " " + m1.LastName
                              select new { emp.Title, emp.FirstName, emp.LastName, manager }).ToList();

                //// Üstteki çözüm inner join olduğu için manageri null olanları almıyor. left outer join yapmalıyız.

                var query7leftouter = (from emp in db.Employees
                                       join m1 in db.Employees on emp.ReportsTo equals m1.EmployeeID into m2
                                       from m in m2.DefaultIfEmpty()
                                       select new
                                       {
                                           emp.Title,
                                           emp.FirstName,
                                           emp.LastName,
                                           ManagerTitle = (m == null) ? string.Empty : m.Title,
                                           ManagerFirstName = (m == null) ? string.Empty : m.FirstName,
                                           ManagerLastName = (m == null) ? string.Empty : m.LastName,
                                       }).ToList();
                ////dgv.DataSource = query7;


                ////8.Give the customer name, the product name and the supplier name for customers
                ////who live in London and suppliers whose name is ‘Pavlova, Ltd.’ or ‘Karkki Oy’.

                var query8 = (from c in db.Customers
                              join ord in db.Orders on c.CustomerID equals ord.CustomerID
                              join od in db.Order_Details on ord.OrderID equals od.OrderID
                              join p in db.Products on od.ProductID equals p.ProductID
                              join sup in db.Suppliers on p.SupplierID equals sup.SupplierID
                              where c.City == "London" && (sup.CompanyName == "Pavlova, Ltd." || sup.CompanyName == "Karkki Oy")
                              select new { c.CompanyName, p.ProductName, SupplierName = sup.CompanyName, c.City }).ToList();

                ////dgv.DataSource = query8;



                ////9.Give the name of products that were bought or sold by people who live in London.
                ////Write two versions of the query, with and without union.

                var query9union = ((from c in db.Customers
                                    join ord in db.Orders on c.CustomerID equals ord.CustomerID
                                    join od in db.Order_Details on ord.OrderID equals od.OrderID
                                    join p in db.Products on od.ProductID equals p.ProductID
                                    select new { p.ProductName })
                             .Union(from emp in db.Employees
                                    join order in db.Orders on emp.EmployeeID equals order.EmployeeID
                                    join od2 in db.Order_Details on order.OrderID equals od2.OrderID
                                    join prod in db.Products on od2.ProductID equals prod.ProductID
                                    select new { prod.ProductName })).Distinct().ToList();

                //// without union :

                var query9 = (from c in db.Customers
                              join ord in db.Orders on c.CustomerID equals ord.CustomerID
                              join od in db.Order_Details on ord.OrderID equals od.OrderID
                              join p in db.Products on od.ProductID equals p.ProductID
                              join emp in db.Employees on ord.EmployeeID equals emp.EmployeeID
                              where c.City == "London" || emp.City == "London"
                              select new { p.ProductName }).Distinct().ToList();



                //dgv.DataSource = query9;

                //10.Give the names of employees who are strictly older than:
                //(a)an employee who lives in London.
                //(b) any employee who lives in London.

                //  Note :  for aging calculation min birthdate is older than anyone!!!

                var query10a = (from emp in db.Employees
                                where emp.BirthDate < (from e2 in db.Employees.Where(a => a.City == "London")
                                                       select e2.BirthDate).Max()
                                select new { emp.FirstName, emp.LastName, emp.BirthDate }).ToList();

                var query10b = (from emp in db.Employees
                                where emp.BirthDate < (from e2 in db.Employees.Where(a => a.City == "London")
                                                       select e2.BirthDate).Min()
                                select new { emp.FirstName, emp.LastName, emp.BirthDate }).ToList();


                //dgv.DataSource = query10b;

                //11.Give the name of employees who work longer than any employee of London.

                var query11 = (from emp in db.Employees
                               where emp.HireDate < (from emp2 in db.Employees.Where(a => a.City == "London")
                                                     select emp2.HireDate).Min()
                               select new { emp.FirstName, emp.LastName, emp.HireDate }).ToList();

                //dgv.DataSource = query11;

                //12.Give the name of employees and the city where they live for employees who have
                //sold to customers in the same city.

                var query12 = (from emp in db.Employees
                               join ord in db.Orders on emp.EmployeeID equals ord.OrderID
                               join c in db.Customers on ord.CustomerID equals c.CustomerID
                               where emp.City == c.City
                               select new { emp.FirstName, emp.LastName, emp.City }).ToList();

                //dgv.DataSource = query12;

                //13.Give the name of customers who have not purchased any product.

                var query13 = (from c in db.Customers
                               where !c.Orders.Any()
                               select new { c.CompanyName }).ToList();

                var query13b = (from c in db.Customers
                                where c.Orders.Count() == 0
                                select new { c.CompanyName }).ToList();

                //dgv.DataSource = query13;

                //14.Give the name of customers who bought all products with price less than 5.

                var query14 = (from c in db.Customers
                               let allProducts = from p in db.Products.Where(x => x.UnitPrice < 5)
                                                 select p.ProductID
                               where !allProducts.Except(
                                               from ord in c.Orders
                                               from od in ord.Order_Details
                                               select od.ProductID).Any()
                               select new { c.CompanyName }).ToList();

                //dgv.DataSource = query14;

                //15.Give the name of the products sold by all employees.

                var query15 = (from p in db.Products
                               let allEmployees = from emp in db.Employees select emp.EmployeeID
                               where !allEmployees.Except(
                                   from od in db.Order_Details
                                   join o in db.Orders on od.OrderID equals o.OrderID
                                   join emp in db.Employees on o.EmployeeID equals emp.EmployeeID
                                   select emp.EmployeeID).Any()
                               select new { p.ProductName }).ToList();

                //dgv.DataSource = query15;


                //16.Give the name of customers who bought all products purchased by the customer
                //whose identifier is ‘LAZYK’

                var query16 = (from c in db.Customers
                               where c.CustomerID != "LAZYK"
                               let allProductsCustomers = from o in c.Orders
                                                          from od in o.Order_Details
                                                          join p in db.Products on od.ProductID equals p.ProductID
                                                          select p.ProductID
                               let allProductsLazyk = from o in c.Orders
                                                      from od in o.Order_Details
                                                      join p in db.Products on od.ProductID equals p.ProductID
                                                      where c.CustomerID == "LAZYK"
                                                      select p.ProductID
                               where !allProductsLazyk.Except(allProductsCustomers).Any()
                               select new { c.CustomerID, c.CompanyName }).ToList();

                //dgv.DataSource = query16;

                //17.Give the name of customers who bought exactly the same products as the customer
                //whose identifier is ‘LAZYK’

                var query17 = (from c in db.Customers
                               where c.CustomerID != "LAZYK"
                               let allProductsCustomers = from o in c.Orders
                                                          from od in o.Order_Details
                                                          join p in db.Products on od.ProductID equals p.ProductID
                                                          select p.ProductID
                               let allProductsByLazyk = from o in c.Orders
                                                        from od in o.Order_Details
                                                        join p in db.Products on od.ProductID equals p.ProductID
                                                        where c.CustomerID == "LAZYK"
                                                        select p.ProductID
                               where !allProductsByLazyk.Except(allProductsCustomers).Any()
                               where !allProductsCustomers.Except(allProductsByLazyk).Any()
                               select new { c.CustomerID, c.CompanyName }).ToList();

                // dgv.DataSource = query17;


                //18.Give the average price of products by category.

                var query18 = (from p in db.Products
                               join c in db.Categories on p.CategoryID equals c.CategoryID
                               group new { c, p } by new { c.CategoryID, c.CategoryName } into g
                               select new {
                                   CategoryId = g.Key.CategoryID,
                                   CategoryName = g.Key.CategoryName,
                                   AveragePrice = g.Average(x => x.p.UnitPrice)
                               }).ToList();

                //dgv.DataSource = query18;


                //19.Given the name of the categories and the average price of products in each category.

                // i have already answered this question on question 18 ! 
                // this answer from that pdf!

                var query19 = (from P in db.Products
                               join C in db.Categories on P.CategoryID equals C.CategoryID
                               group P by P.Categories.CategoryName into categProds
                               select new
                               {
                                   categProds.Key,
                                   AvgPrice = categProds.Average(C => C.UnitPrice)
                               }).ToList();
                //dgv.DataSource = query19;

                //20.Give the identifier and the name of the companies that provide more than 3 products.

                var query20 = (from s in db.Suppliers
                               join p in db.Products on s.SupplierID equals p.ProductID
                               where s.Products.Count() > 3
                               select new { s.SupplierID, s.CompanyName, ProductCount = s.Products.Count() }).ToList();

                // dgv.DataSource = query20;


                //21.Give the identifier, name, and number of orders of employees, ordered by the employee identifier.

                //var query21 = (from emp in db.Employees
                //               join o in db.Orders on emp.EmployeeID equals o.EmployeeID
                //               group new { emp, o } by new { emp.EmployeeID,emp.FirstName,emp.LastName } into g
                //               select new
                //               {
                //                   EmpId=g.Key.EmployeeID,
                //                   EmpName = g.Key.FirstName + " " + g.Key.LastName,
                //                   NumberOfOrders = g.Select(x => x.o.OrderID).Count()
                //               }).ToList();

                // misunderstanding :( , just order

                var query21 = (from emp in db.Employees
                               orderby emp.EmployeeID
                               select new { emp.EmployeeID, emp.FirstName, emp.LastName, NumberOfOrders=emp.Orders.Count() }).ToList();
                //dgv.DataSource = query21;


                //22.For each employee give the identifier, name, and the number of distinct products
                //sold, ordered by the employee identifier.

                var query22 = (from emp in db.Employees
                               orderby emp.EmployeeID
                               select new {
                                   emp.EmployeeID,
                                   emp.FirstName,
                                   emp.LastName,
                                   NumberOfDisctinctProducts = (from ord in emp.Orders
                                                               from od in ord.Order_Details
                                                               join p in db.Products on od.ProductID
                                                               equals p.ProductID
                                                               select new {p.ProductID}).Distinct().Count()
                               }).ToList();


                // dgv.DataSource = query22;

                //23.Give the identifier, name, and total sales of employees, ordered by the employee
                //identifier.

                var query23 = (from emp in db.Employees
                               orderby emp.EmployeeID
                               let totalSales = from ord in emp.Orders
                                                from od in ord.Order_Details
                                                select new
                                                {
                                                    od.Quantity,
                                                    od.UnitPrice,
                                                    od.Discount
                                                }
                               select new
                               {
                                   emp.EmployeeID,
                                   emp.FirstName,
                                   emp.LastName,
                                   TotalSales = totalSales
                               }).AsEnumerable().Select(x => new
                               {
                                   EmpId = x.EmployeeID,
                                   EmpFirstName = x.FirstName,
                                   EmpLastName = x.LastName,
                                   Total = x.TotalSales.Select(y=>Convert.ToDecimal(y.Quantity)*Convert.ToDecimal(y.UnitPrice)*(1-Convert.ToDecimal(y.Discount))).Sum()
                               }).ToList();

                // dgv.DataSource = query23;

                //24.Give the identifier, name, and total sales of employees, ordered by the employee
                //identifier for employees who have sold more than 70 different products.

                var query24 = (from emp in db.Employees
                               orderby emp.EmployeeID
                               let soldedProds=    (from ord in emp.Orders
                                                   from od in ord.Order_Details
                                                   join p in db.Products on od.ProductID equals p.ProductID
                                                   select p.ProductID).Distinct().Count()
                               where soldedProds>70
                               let totalSales = from ord in emp.Orders
                                                from od in ord.Order_Details
                                                select new
                                                {
                                                    od.Quantity,
                                                    od.UnitPrice,
                                                    od.Discount
                                                }
                               select new
                               {
                                   emp.EmployeeID,
                                   emp.FirstName,
                                   emp.LastName,
                                   TotalSales = totalSales
                               }).AsEnumerable().Select(x => new
                               {
                                   EmpId = x.EmployeeID,
                                   EmpFirstName = x.FirstName,
                                   EmpLastName = x.LastName,
                                   Total = x.TotalSales.Select(y => Convert.ToDecimal(y.Quantity) * Convert.ToDecimal(y.UnitPrice) * (1 - Convert.ToDecimal(y.Discount))).Sum()
                               }).ToList();

                // dgv.DataSource = query24;


                //25.Give the names of employees who sell the products of more than 7 suppliers.

                var query25 = (from emp in db.Employees
                               let prodSuppliers = (from ord in emp.Orders
                                                    from od in ord.Order_Details
                                                    join p in db.Products on od.ProductID equals p.ProductID
                                                    join s in db.Suppliers on p.SupplierID equals s.SupplierID
                                                    select s.SupplierID).Distinct().Count()
                               where prodSuppliers > 7
                               select new { emp.FirstName, emp.LastName }).ToList();

                // dgv.DataSource = query25;


                //26.Give the customer name and the product name such that the quantity of this product
                //bought by the customer in a single order is more than 5 times the average quantity
                //of this product bought in a single order among all clients.

                var query26 = (from c in db.Customers
                               from o in c.Orders
                               from od in o.Order_Details
                               join p in db.Products on od.ProductID equals p.ProductID
                               let prodsAvg = (
                                                  from odetails in db.Order_Details
                                                  where odetails.ProductID==p.ProductID
                                                  select odetails.Quantity).Cast<int>().Average()
                               where od.Quantity > 5 * prodsAvg
                               select new { c.CompanyName, p.ProductName }).ToList();

                // dgv.DataSource = query26;



            }
        }

        private void dgv_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
