﻿// <auto-generated />
using System;
using Fumo.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fumo.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Fumo.Database.ChannelDTO", b =>
                {
                    b.Property<string>("TwitchID")
                        .HasColumnType("text");

                    b.Property<DateTime>("DateJoined")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Setting[]>("Settings")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("jsonb")
                        .HasDefaultValueSql("'[]'::jsonb");

                    b.Property<string>("TwitchName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UserTwitchID")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("TwitchID");

                    b.HasIndex("TwitchID")
                        .IsUnique();

                    b.HasIndex("UserTwitchID")
                        .IsUnique();

                    b.ToTable("Channels");
                });

            modelBuilder.Entity("Fumo.Database.UserDTO", b =>
                {
                    b.Property<string>("TwitchID")
                        .HasColumnType("text");

                    b.Property<DateTime>("DateSeen")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("now()");

                    b.Property<Setting[]>("Settings")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("jsonb")
                        .HasDefaultValueSql("'[]'::jsonb");

                    b.Property<string>("TwitchName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string[]>("UsernameHistory")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text[]")
                        .HasDefaultValueSql("'{}'::text[]");

                    b.HasKey("TwitchID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Fumo.Database.ChannelDTO", b =>
                {
                    b.HasOne("Fumo.Database.UserDTO", "User")
                        .WithMany()
                        .HasForeignKey("UserTwitchID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
