﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using YeOldeLinkDetector;

#nullable disable

namespace yeoldelinkdetector.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.2");

            modelBuilder.Entity("YeOldeLinkDetector.Message", b =>
                {
                    b.Property<string>("MessageId")
                        .HasColumnType("TEXT");

                    b.Property<string>("AuthorName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ChannelId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("MessageId");

                    b.HasIndex("Url", "ChannelId", "Timestamp");

                    b.ToTable("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
