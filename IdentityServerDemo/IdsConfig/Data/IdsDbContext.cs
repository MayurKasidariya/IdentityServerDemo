using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdsConfig.Data
{
    public class IdsDbContext : IdentityDbContext
    {
        public IdsDbContext(DbContextOptions<IdsDbContext> options)
      : base(options)
        {
        }
    }
}
