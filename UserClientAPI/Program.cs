using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UserClientAPI.Data;
using UserClientAPI.Models;

namespace UserClientAPI
{
    class Program
    {
        HttpClient client = new HttpClient();
       
        static async Task Main(string[] args)
        {
            Program program = new Program();

            #region Create User object
            //att skicka in vid registreting
            RegisterUser registerEmployee = CreateUser("Jerry", "Davidson",  "testEmployee", "Secret1337?!", "Germany", "Employee");
            RegisterUser registerManager =  CreateUser("Elly", "Thompson",   "testManager", "Secret1337?!", "USA", "CountryManager");
            RegisterUser registerVD =       CreateUser("Bob", "Adamsson",    "testVD", "Secret1337?!", "France", "VD");
            RegisterUser registerAdmin =    CreateUser("John", "Doe",        "testAdmin", "Secret1337?!", "Spain", "Admin");
            //att skicka in vid Login
            LoginUser admin = CreateLoginUser(registerAdmin);
            LoginUser VD = CreateLoginUser(registerVD);
            LoginUser manager = CreateLoginUser(registerManager);
            LoginUser employee = CreateLoginUser(registerEmployee);
            #endregion
            #region Register users
            await program.RegisterUser(registerAdmin);
            await program.RegisterUser(registerVD);
            await program.RegisterUser(registerManager);
            await program.RegisterUser(registerEmployee);
            #endregion
            #region Login users
            User adminUser = await program.Login(admin);
            User VDUser= await program.Login(VD);
            User ManagerUser = await program.Login(manager);
            User EmployeeUser =await program.Login(employee);
            #endregion
       
            #region Get and update Employee/s
            int employeeID = int.Parse(EmployeeUser.EmployeeId);
            Console.Write("Getting " + EmployeeUser.UserName);
            User user1 = await program.GetUser(adminUser, employeeID);
            Console.Write("Getting " + EmployeeUser.UserName);
            user1 = await program.GetUser(VDUser, employeeID);
            Console.Write("Getting " + EmployeeUser.UserName);
            user1 = await program.GetUser(ManagerUser, employeeID);
            Console.Write("Getting " + EmployeeUser.UserName);
            user1 = await program.GetUser(EmployeeUser, employeeID);


            Console.Write("Updating " + EmployeeUser.UserName);
            user1 = await program.UpdateUser(adminUser, employeeID);
            Console.Write("Updating " + EmployeeUser.UserName);
            user1 = await program.UpdateUser(VDUser, employeeID);
            Console.Write("Updating " + EmployeeUser.UserName);
            user1 = await program.UpdateUser(ManagerUser, employeeID);
            Console.Write("Updating " + EmployeeUser.UserName);
            user1 = await program.UpdateUser(EmployeeUser, employeeID);


            Console.WriteLine("Get ALL users by " + adminUser.UserName);
            await program.GetAll(adminUser);
            Console.WriteLine("Get ALL users by " + VDUser.UserName);
            await program.GetAll(VDUser);
            Console.WriteLine("Get ALL users by " + ManagerUser.UserName);
            await program.GetAll(ManagerUser);
            Console.WriteLine("Get ALL users by " + EmployeeUser.UserName);
            await program.GetAll(EmployeeUser);
            
            #endregion
            //gets three users from NW and adds them to Identity for Orders testing
            #region Shamefur Dispray
            Console.WriteLine("skapar tre användare från Northwind och kopplar ihop de");
            await program.Sync();
            List<User> newUsers= await program.AddManualy();
            List<LoginUser> loginUsers =  program.ConverToLoginUsers(newUsers);
            Console.WriteLine("Loggar in som de tre nya användare");
            User testEmployee = await program.Login(employee);
            User Margaret = await program.Login(loginUsers[0]);
            User Steven = await program.Login(loginUsers[1]);
            User Michael= await program.Login(loginUsers[2]);
            #endregion
            #region Orders part



            Console.WriteLine("TestEmployee is getting his orders");
            await program.GetMyOrders(testEmployee, testEmployee);
            Console.WriteLine("Margaret is getting her orders");
            await program.GetMyOrders(Margaret, Margaret);
            Console.WriteLine("Steven is getting Margaret's orders");
            await program.GetMyOrders(Margaret, Steven);
            Console.WriteLine("Admin is getting Margaret's orders");
            await program.GetMyOrders(Margaret, adminUser);

            
            Console.WriteLine("Getting country orders as manager from USA");
            await program.GetCountryOrders(ManagerUser, ManagerUser.Country);
            Console.WriteLine("Getting country orders as vd");
            await program.GetCountryOrders(VDUser, VDUser.Country);
            Console.WriteLine("Getting country orders as manager for wrong country");
            await program.GetCountryOrders(ManagerUser, "China");
            Console.WriteLine("Getting all orders as Margret");
            await program.GetAllOrders(Margaret);
            Console.WriteLine("Getting all orders as manager, RawSql");
            await program.GetAllOrders(ManagerUser);
            Console.WriteLine("Getting all orders as admin");
            await program.GetAllOrders(VDUser);
            Console.WriteLine("Delete employee as vd");
            await program.DeleteEmployee(VDUser, int.Parse(EmployeeUser.EmployeeId));
            Console.WriteLine("Delete employee as admin");
            await program.DeleteEmployee(adminUser,int.Parse(EmployeeUser.EmployeeId));
            #endregion
            
        }

        private async Task<User> GetUserByName(string userName)
        {
            User user = new User();
            using (var context = new AppDBContext())
            {
                var userInDB = await context.Users.Where(u=>u.UserName==userName).FirstOrDefaultAsync();
                Console.WriteLine(userInDB.UserName);
                if (userInDB != null)
                {
                    user.Id = userInDB.Id;
                    user.Country = user.Country;
                    user.EmployeeId = userInDB.EmployeeId;
                    user.JwtExpiresAt = userInDB.JwtExpiresAt;
                    user.JwtToken = userInDB.JwtToken;
                    user.RefExpiresAt = userInDB.RefExpiresAt;
                    user.RefreshToken = userInDB.RefreshToken;
                    user.UserName = userInDB.UserName;
                    return user;
                }
            }
            Console.WriteLine("Having troubles retriving " + userName);
            return null;
        }
        private async Task<User> DeleteEmployee(User user, int employeeId)
        {
            if (!IsValid(user))
            {
                user = await RefreshTokens(user);
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            var response = await client.DeleteAsync("https://localhost:5001/api/employees/" + employeeId);
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            else
            {
                using (var context = new AppDBContext())
                {
                    string id = employeeId.ToString();
                    var userInDB = context.Users.FirstOrDefault(u => u.EmployeeId.Equals(id));
                    if (userInDB != null)
                    {
                        var deleted = context.Users.Remove(userInDB);
                        context.SaveChanges();
                        Console.WriteLine(userInDB.UserName + " deleted");
                    }
                }
            }
            string resultContent = response.Content.ReadAsStringAsync().Result;
            User result = JsonConvert.DeserializeObject<User>(resultContent);
            Console.WriteLine(resultContent);
            return result;
        }

        #region Create User objects

        static RegisterUser CreateUser(string firstName, string lastName, string userName, string password, string country, string role)
        {
            RegisterUser registerUser = new RegisterUser();
            registerUser.FirstName = firstName;
            registerUser.LastName = lastName;
            registerUser.UserName = userName;
            registerUser.Password = password;
            registerUser.Country = country;
            registerUser.Role = role;
            return registerUser;
        }
        static LoginUser CreateLoginUser(RegisterUser registerUser)
        {
            LoginUser loginUser = new LoginUser();
            loginUser.UserName = registerUser.UserName;
            loginUser.Password = registerUser.Password;
            return loginUser;
        }
        #endregion

        private async Task<User> RegisterUser(RegisterUser user)
        {
            Console.WriteLine("Registering: " + user.UserName + " " + user.Role);
            var postUser = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://localhost:5001/api/employees/register", postUser);
            string resultContent = response.Content.ReadAsStringAsync().Result;
            LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(resultContent);
            Console.WriteLine(resultContent);
            if (response.IsSuccessStatusCode)
            {
                User saveToDb = new User();
                saveToDb.UserName = user.UserName;
                saveToDb.JwtToken = "";
                saveToDb.RefreshToken = "";
                saveToDb.JwtExpiresAt = DateTime.Now;
                saveToDb.RefExpiresAt = DateTime.Now;
                saveToDb.Country = user.Country;
                saveToDb.EmployeeId = loginResponse.employeeId;
                await CheckIfUserExists(saveToDb);
                return saveToDb;
            }
            return null;
        }

        #region Get and update Employee/s
        private async Task<User> GetUser(User user,int employeeId)
        {
            if (!IsValid(user))
            {
                user = await RefreshTokens(user);
            }
            Console.WriteLine(" by " + user.UserName);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            var response = await client.GetAsync("https://localhost:5001/api/employees/" + employeeId);
            string resultContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            User result = JsonConvert.DeserializeObject<User>(resultContent);
            return result;
        }
        private async Task<User> UpdateUser(User user, int employeeId)
        {
            if (!IsValid(user))
            {
                user=await RefreshTokens(user);
            }
            User userToUpdate = new User();
            Console.WriteLine(" by " + user.UserName);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            using (var context = new AppDBContext())
            {
                userToUpdate = context.Users.FirstOrDefault(u => u.EmployeeId == employeeId.ToString());
            }
            var jsonUser = new StringContent(JsonConvert.SerializeObject(userToUpdate), Encoding.UTF8, "application/json");
            var response = await client.PutAsync("https://localhost:5001/api/employees/" + employeeId, jsonUser);
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            string resultContent = response.Content.ReadAsStringAsync().Result;
            User result = JsonConvert.DeserializeObject<User>(resultContent);
            return result;
        }
        private async Task<string> GetAll(User user)
        {
            if (!IsValid(user))
            {
                user = await RefreshTokens(user);
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            var response = await client.GetAsync("https://localhost:5001/api/Employees");
            string resultContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(response.ReasonPhrase);
            return resultContent;
        }
        #endregion

        #region Orders part
        private async Task<HttpResponseMessage> GetMyOrders(User user, User issuer)
        {
            if (!IsValid(issuer))
            {
                issuer = await RefreshTokens(user);
            }
            int employeeId = int.Parse(user.EmployeeId);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", issuer.JwtToken);
            var response = await client.GetAsync("https://localhost:5001/api/orders/" + employeeId);
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return response;
            }
            string resultContent = response.Content.ReadAsStringAsync().Result;
            var status = response.Content.ReadAsStringAsync().Status;
            //Console.WriteLine(resultContent);
            return response;
        }

        private async Task<HttpResponseMessage> GetCountryOrders(User user, string country)
        {
            if (!IsValid(user))
            {
                user = await RefreshTokens(user);
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            var response = await client.GetAsync("https://localhost:5001/api/orders/" + country);
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return response;
            }
            var resultContent = response.Content.ReadAsStringAsync().Status;
            Console.WriteLine(resultContent);
            return response;
        }

        private async Task<HttpResponseMessage> GetAllOrders(User user)
        {
            if (!IsValid(user))
            {
                user = await RefreshTokens(user);
            }
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.JwtToken);
            var response = await client.GetAsync("https://localhost:5001/api/orders/");
            Console.WriteLine(response.ReasonPhrase);
            if (!response.IsSuccessStatusCode)
            {
                return response;
            }
            string resultContent = response.Content.ReadAsStringAsync().Result;
            List<Orders> results = JsonConvert.DeserializeObject<List<Orders>>(resultContent);
            return response;
        }


        #endregion

        public static async Task<bool> CheckIfUserExists(User user)
        {
            using (var context = new AppDBContext())
            {
                User userInDB = await context.Users.Where(u=>u.UserName==user.UserName).FirstOrDefaultAsync();
                if (userInDB == null)
                {
                    context.Update(user);
                    context.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }

        private async Task<List<int>> CheckForExpired(User user)
        {
            List<int> expires = new List<int>();
            using (var context = new AppDBContext())
            {
                var userRecord = await context.Users.Where(u => u.UserName == user.UserName).FirstOrDefaultAsync();
                DateTime currentTime = DateTime.Now;
                if (userRecord.JwtExpiresAt != null && userRecord.RefExpiresAt != null)
                {
                    expires.Add(userRecord.JwtExpiresAt.CompareTo(currentTime));
                    expires.Add(userRecord.RefExpiresAt.CompareTo(currentTime));
                }
                else
                {
                    Console.WriteLine("Token expire dates are missing");
                    expires.Add(-1);
                    expires.Add(-1);
                }
            }
            return expires;
        }

        private async Task<User> RefreshTokens(User user)
        {
            List<int> expiredTokensCheck =  await CheckForExpired(user);
            //om tokens saknas eller refreshtoken är expired logga in på nytt
            if (user.JwtToken != null && user.RefreshToken!=null && expiredTokensCheck[1]>0 )
            {
                UserRefreshToken refToken = new UserRefreshToken();
                refToken.RefreshToken = user.RefreshToken;
                var jsonUser = new StringContent(JsonConvert.SerializeObject(refToken), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://localhost:5001/api/employees/refresh-token", jsonUser);
                string resultContent = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Refreshing " + response.ReasonPhrase);
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(resultContent);
                if (response.IsSuccessStatusCode)
                {
                    using (var context = new AppDBContext())
                    {
                        User saveToDb = context.Users.FirstOrDefault(u => u.UserName == loginResponse.UserName);
                        saveToDb.UserName = loginResponse.UserName;
                        saveToDb.JwtToken = loginResponse.JwtToken;
                        saveToDb.JwtExpiresAt = loginResponse.JwtExpiresAt;
                        saveToDb.RefreshToken = loginResponse.RefreshToken;
                        saveToDb.RefExpiresAt = loginResponse.RefExpiresAt;
                        context.SaveChanges();
                    }
                    Console.WriteLine("Updating tokens for " + loginResponse.UserName);
                    Console.WriteLine("Previous refresh token: ");
                    Console.WriteLine(user.RefreshToken);
                    Console.WriteLine("Current refresh token: ");
                    Console.WriteLine(loginResponse.RefreshToken);
                }
                else
                {
                    Console.WriteLine(resultContent);
                }
                User savedUser = await GetUserByName(user.UserName);
                return savedUser;
            }
            Console.WriteLine("Token value is null or refToken is expired");
            LoginUser loginUser = new LoginUser();
            loginUser.UserName = user.UserName;
            loginUser.Password = "Secret1337?!";
            User updatedUser = await Login(loginUser);
            return updatedUser;
        }
        private bool IsValid(User user)
        {
            //Console.WriteLine("Checking if " + user.UserName + "'s Tokens are valid");
            bool valid = false;
            if (user.JwtExpiresAt >= DateTime.UtcNow || user.JwtExpiresAt >= DateTime.UtcNow)
            {
                valid = true;
            }
            return valid;
        }

        private async Task<User> Login(LoginUser user)
        {
            Console.WriteLine("Logging in:" + user.UserName);
            var jsonUser = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://localhost:5001/api/employees/Login", jsonUser);
            string resultContent = response.Content.ReadAsStringAsync().Result;
            var loginResponse =  JsonConvert.DeserializeObject<LoginResponse>(resultContent);
            Console.WriteLine(response.ReasonPhrase);
            Console.WriteLine(resultContent);
            if (response.IsSuccessStatusCode)
            {
                using (var context = new AppDBContext())
                {
                    User saveToDb = context.Users.FirstOrDefault(u => u.UserName == user.UserName);
                    saveToDb.JwtToken = loginResponse.JwtToken;
                    saveToDb.JwtExpiresAt = loginResponse.JwtExpiresAt;
                    saveToDb.RefreshToken = loginResponse.RefreshToken;
                    saveToDb.RefExpiresAt = loginResponse.RefExpiresAt;
                    context.SaveChanges();
                    return saveToDb;
                }
            }
            return null;
        }

        #region Shamefur Dispray
        private List<LoginUser> ConverToLoginUsers(List<User> users)
        {
            LoginUser one = new LoginUser();
            one.Password = "Secret1337?!";
            one.UserName = users[0].UserName;
            LoginUser two = new LoginUser();
            two.Password = "Secret1337?!";
            two.UserName = users[1].UserName;
            LoginUser three = new LoginUser();
            three.Password = "Secret1337?!";
            three.UserName = users[2].UserName;
            List<LoginUser> loginUsers = new List<LoginUser>();
            loginUsers.Add(one);
            loginUsers.Add(two);
            loginUsers.Add(three);
            return loginUsers;
        }

        private async Task<List<User>> AddManualy()
        {
            User user1 = new User();
            user1.EmployeeId = 4.ToString();
            user1.Country = "USA";
            user1.JwtToken = "";
            user1.RefreshToken = "";
            user1.JwtExpiresAt = DateTime.Now;
            user1.RefExpiresAt = DateTime.Now;
            user1.UserName = "MargaretPeacock";
            User user2 = new User();
            user2.EmployeeId = 5.ToString();
            user2.Country = "UK";
            user2.JwtToken = "";
            user2.RefreshToken = "";
            user2.JwtExpiresAt = DateTime.Now;
            user2.RefExpiresAt = DateTime.Now;
            user2.UserName = "StevenBuchanan";
            User user3 = new User();
            user3.EmployeeId = 6.ToString();
            user3.Country = "UK";
            user3.JwtToken = "";
            user3.RefreshToken = "";
            user3.JwtExpiresAt = DateTime.Now;
            user3.RefExpiresAt = DateTime.Now;
            user3.UserName = "MichaelSuyama";
            List<User> userList = new List<User>();
            userList.Add(user1);
            userList.Add(user2);
            userList.Add(user3);

            using (var context = new AppDBContext())
            {
                foreach (User u in userList)
                {
                    User found = await context.Users.Where(e => e.EmployeeId == u.EmployeeId).FirstOrDefaultAsync();
                    if (found == null)
                    {
                        context.Add(u);
                    }
                }
                context.SaveChanges();
            }
            return userList;
        }
        //creates 3 users from NW in Identity
        private async Task<string> Sync()
        {
            var response = await client.PostAsync("https://localhost:5001/api/employees/sync", null);
            string resultContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(resultContent);
            return resultContent;
        }
        #endregion
    }
}
   