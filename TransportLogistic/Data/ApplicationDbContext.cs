using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TransportLogistic.Models;

namespace TransportLogistic.Data;

public partial class ApplicationDbContext : IdentityDbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<TransportLogistic.Models.Route> Routes { get; set; }

    public virtual DbSet<Transport> Transports { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_cityes");

            entity.ToTable("cities");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .HasColumnName("region");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Orders");

            entity.ToTable("orders");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Price)
                .HasColumnType("money")
                .HasColumnName("price");
            entity.Property(e => e.SeatNumber).HasColumnName("seatNumber");
            entity.Property(e => e.Stasus)
                .HasMaxLength(20)
                .HasColumnName("stasus");
            entity.Property(e => e.Trip).HasColumnName("trip");
            entity.Property(e => e.User)
                .HasMaxLength(450)
                .HasColumnName("user");

            entity.HasOne(d => d.TripNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.Trip)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_trips");

            // Добавьте связь с пользователем
            entity.HasOne(e => e.UserNavigation)
                .WithMany()
                .HasForeignKey(e => e.User)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_AspNetUsers");
        });

        modelBuilder.Entity<TransportLogistic.Models.Route>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Distance).HasColumnName("distance");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Start).HasColumnName("start");
            entity.Property(e => e.Stop).HasColumnName("stop");

            entity.HasOne(d => d.StartNavigation).WithMany(p => p.RouteStartNavigations)
                .HasForeignKey(d => d.Start)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Routes_cities");

            entity.HasOne(d => d.StopNavigation).WithMany(p => p.RouteStopNavigations)
                .HasForeignKey(d => d.Stop)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Routes_cities1");
        });

        modelBuilder.Entity<Transport>(entity =>
        {
            entity.ToTable("transports");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CarNumber)
                .HasMaxLength(10)
                .HasColumnName("car_number");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasColumnName("model");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.ToTable("trips");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ArrivalTime)
                .HasColumnType("datetime")
                .HasColumnName("arrival_time");
            entity.Property(e => e.Conductor)
                .HasMaxLength(450)
                .HasColumnName("conductor");
            entity.Property(e => e.DepatureTime)
                .HasColumnType("datetime")
                .HasColumnName("depature_time");
            entity.Property(e => e.Driver)
                .HasMaxLength(450)
                .HasColumnName("driver");
            entity.Property(e => e.Route).HasColumnName("route");
            entity.Property(e => e.Transport).HasColumnName("transport");

            entity.HasOne(d => d.RouteNavigation).WithMany(p => p.Trips)
                .HasForeignKey(d => d.Route)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_trips_Routes");

            entity.HasOne(d => d.TransportNavigation).WithMany(p => p.Trips)
                .HasForeignKey(d => d.Transport)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_trips_transports");

            // Связь с водителем (Driver)
            entity.HasOne(d => d.DriverNavigation)
                .WithMany()
                .HasForeignKey(d => d.Driver)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_trips_AspNetUsers_Driver");

            // Связь с кондуктором (Conductor) - опционально
            entity.HasOne(d => d.ConductorNavigation)
                .WithMany()
                .HasForeignKey(d => d.Conductor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_trips_AspNetUsers_Conductor");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
