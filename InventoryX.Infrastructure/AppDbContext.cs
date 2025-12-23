using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using InventoryX.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt)
        {
        }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<SaleGroup> SaleGroups { get; set; }
        public DbSet<InventoryItemType> InventoryItemTypes { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<RetailStock> RetailStock { get; set; }
        public override DbSet<User> Users { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<InventoryItem>().HasOne(e => e.Type).WithMany().HasForeignKey(e => e.TypeId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>().HasOne(e => e.SaleGroup).WithMany().HasForeignKey(e => e.SaleGroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Sale>()
            .HasOne(s => s.Seller)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Sale>()
            .HasOne(s => s.InventoryItem)
            .WithMany()
            .HasForeignKey(s => s.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Purchase>().HasOne(e => e.InventoryItem).WithMany().HasForeignKey(s => s.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Purchase>().HasOne(e => e.Purchaser).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
