using Ftareqi.Domain.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ftareqi.Application.Interfaces.Repositories
{
	public interface IUnitOfWork: IAsyncDisposable	
	{

		IBaseRepository<Car> Cars { get; }
		IBaseRepository<User> Users {  get; }
		IBaseRepository <RefreshToken > RefreshTokens { get; }
		IBaseRepository <OTP > OTPs { get; }
		IDriverProfileRepository DriverProfiles { get; }
		IBaseRepository <Image> Images { get; }
		IBaseRepository <UserWallet> UserWallets {  get; }
		IBaseRepository <WalletTransaction> WalletTransactions { get; }
		IBaseRepository <PaymentTransaction> PaymentTransactions { get; }


		Task<IDbContextTransaction> BeginTransactionAsync();
		Task<int> SaveChangesAsync();
	}
}
