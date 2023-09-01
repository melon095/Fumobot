﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Fumo.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fumo.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20230828133847_add_permission")]
    partial class add_permission
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

                    b.Property<bool>("SetForDeletion")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false);

                    b.Property<List<Setting>>("Settings")
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

                    b.Property<List<string>>("Permissions")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text[]")
                        .HasDefaultValueSql("'[\"default\"]'::text[]");

                    b.Property<List<Setting>>("Settings")
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