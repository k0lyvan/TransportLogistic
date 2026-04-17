using System.ComponentModel.DataAnnotations;

namespace TransportLogistic.Models
{
    public class PaymentViewModel
    {
        public int OrderId { get; set; }

        [Display(Name = "Сумма к оплате")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "Детали заказа")]
        public string OrderDetails { get; set; } = string.Empty;

        [Display(Name = "Пассажир")]
        public string PassengerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите номер карты")]
        [Display(Name = "Номер карты")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Номер карты должен содержать 16 цифр")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите имя держателя карты")]
        [Display(Name = "Держатель карты")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Используйте латинские буквы")]
        public string CardHolder { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите срок действия")]
        [Display(Name = "Срок действия (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Формат: MM/YY")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите CVV код")]
        [Display(Name = "CVV/CVC")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "CVV должен содержать 3 цифры")]
        [DataType(DataType.Password)]
        public string CVV { get; set; } = string.Empty;
    }
}
