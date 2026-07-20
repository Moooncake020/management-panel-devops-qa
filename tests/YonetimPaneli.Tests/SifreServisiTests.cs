using Microsoft.AspNetCore.Identity;
using YonetimPaneli.Models;
using YonetimPaneli.Services;




namespace YonetimPaneli.Tests;


public class SifreServisiTests
{
    private readonly PasswordHasher<AppUser> _passwordhasher;
	private readonly SifreServisi _sifreServisi;

	public SifreServisiTests(){

		_passwordhasher= new PasswordHasher<AppUser>();
		_sifreServisi = new SifreServisi(_passwordhasher);





	}

	[Fact]
	public void HashFormatindaMi_NullDegerVerildiginde_FalsewDonmeli()
	{
		//act
		var sonuc= _sifreServisi.HashFormatindaMi(null);

		//assert

		Assert.False(sonuc);






	}

	[Fact]
	public void HashFormatindaMi_DuzMetinVerildiginde_FalseDonmeli()
	{
		//act
		var sonuc= _sifreServisi.HashFormatindaMi("123456");

		//assert
		Assert.False(sonuc);





	}

	[Fact]
	public void HashFormatindaMi_GecerliIdentityHashVerildiginde_TrueDonmeli()
	{
		//arrange
		var kullanici = new AppUser();
		var hash = _passwordhasher.HashPassword(kullanici, "GuvenliSifre123!");
		//act
		var sonuc = _sifreServisi.HashFormatindaMi(hash);

		//Assert
		Assert.True(sonuc);





	}






}
