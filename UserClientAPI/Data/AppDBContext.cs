using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UserClientAPI.Models;

namespace UserClientAPI.Data
{
    public class AppDBContext:DbContext
    {
        //public AppDBContext(DbContextOptions options) : base(options)
        //{

        //}

        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb; Database = APIClientDB; Trusted_Connection = True; MultipleActiveResultSets = true");
        }
    }
}
