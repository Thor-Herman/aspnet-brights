using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Infrastructure.Security
{
    public class CurrentUser : ICurrentUser
    {
        private IAppDbContext _context;

        public long Identifier { get; private set; }

        public User User { get; private set; }

        public bool IsAuthenticated { get => User != null; }

        public CurrentUser(IAppDbContext context)
        {
            _context = context;
        }

        public async Task SetIdentifier(long identifier)
        {
            Identifier = identifier;
            await Fresh();
        }

        public async Task Fresh()
        {
            User = await _context.Users
                .AsTracking().Where(x => x.Id == Identifier).SingleOrDefaultAsync();
        }
    }
}
