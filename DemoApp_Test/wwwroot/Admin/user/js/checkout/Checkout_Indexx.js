function chuanhoa(name) {
	var chuoi = new String();
	chuoi = name;
	chuoi = chuoi.replace(/ +/g, ' ');
	var arr = chuoi.split(' ');
	for (i = 0; i < arr.length; i++) {
		if (arr[i].length > 1) {
			arr[i] = arr[i].toLowerCase();
			arr1 = arr[i].split('');
			arr1[0] = arr1[0].toUpperCase();
			arr[i] = arr1.join('');
		}
		else {
			arr[i] = arr[i].toUpperCase();
		}
	}
	chuoi = arr.join(' ');
	return chuoi;
}
// -------------------------------------------------------- Kiểm tra tên
function checkten(ten, thongbao) {
	var name = document.getElementById(ten);
	let text = "";
	if (name.value == "") {
		document.getElementById(thongbao).style.color = "Red";
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		if (/[~`!#$%\@^&*+=\-\[\]\\';)(,/{}|\\":<>\?]/g.test(name.value) || /[0-9]/.test(name.value)) {
			text = "Tên không chứa số hoặc ký tự đặc biệt !";
			document.getElementById(thongbao).style.color = "Red";
			document.getElementById(thongbao).innerHTML = text;
		}
		else {
			document.getElementById(thongbao).innerHTML = '<img src="/user/img/viewbill/check.png" style="height:30px;"/>';
			document.getElementById(ten).value = chuanhoa(name.value);
		}
	}

}
// Kiểm tra số điện thoại
function checksdt(ten, thongbao) {
	var sdt = document.getElementById(ten);
	var value = sdt.value;
	let text = "";
	// Kiểm tra rỗng
	if (value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		// Kiểm tra xem giá trị có phải là số hay không
		if (isNaN(value)) {
			text = "Không nhập ký tự vào mục này !";
			document.getElementById(thongbao).innerHTML = text;
			value = value.slice(0, -1);
			document.getElementById(ten).value = value;
		}
		else {
			if (value.length < sdt.maxLength) {
				text = "Số điện thoại cần đủ 10 chữ số !";
				document.getElementById(thongbao).innerHTML = text;
			}
			else {
				document.getElementById(thongbao).innerHTML = '<img src="/user/img/viewbill/check.png" style="height:30px;"/>';
			}
		}
	}
}

// Kiểm tra địa chỉ gmail
function checkgmail(ten, thongbao) {
	// Định nghĩa biểu thức chính quy cho địa chỉ email
	var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
	var email = document.getElementById(ten);
	var value = email.value;
	let text = "";
	// Kiểm tra rỗng
	if (value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		if (emailRegex.test(value)) {
			document.getElementById(thongbao).innerHTML = '<img src="/user/img/viewbill/check.png" style="height:30px;"/>';
		}
		else {
			text = "Gmail của bạn chưa đúng định dạng !";
			document.getElementById(thongbao).innerHTML = text;
		}
	}
}
// Kiểm tra địa chỉ giao hàng
function checkdiachi(ten, thongbao) {
	var diachi = document.getElementById(ten);
	var value = diachi.value;
	// Kiểm tra rỗng
	if (value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		text = "";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById(ten).value = chuanhoa(value);
	}
}
// Kiểm tra hình thức thanh toán
function checkhttt_select(thongbao) {
	// Thêm mã JavaScript để xử lý khi một phương thức thanh toán được chọn
	var chonhinhthuc = document.querySelector('input[name="phuongthucthanhtoan"]:checked');
	// Kiểm tra xem đã chọn hình thức thanh toán nào chưa
	if (!chonhinhthuc) {
		text = "Chưa chọn hình thức thanh toán";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		text = "";
		document.getElementById(thongbao).innerHTML = text;
	}
}
function checkhttt_button(thongbao) {
	// Thêm mã JavaScript để xử lý khi một phương thức thanh toán được chọn
	var chonhinhthuc = document.querySelector('input[name="phuongthucthanhtoan"]:checked');
	var ngaydh = document.getElementById('ngaydathang').value;
	var pttt = document.getElementById('phuongthucthanhtoan').value;
	var diachigh = document.getElementById('diachigh').value;
	var tennd = document.getElementById('TenNguoidung').value;
	var sdt = document.getElementById('SodienthoaiNv').value;
	var tongtien = document.getElementById('tongtienhoadon').textContent;
	// Kiểm tra xem đã chọn hình thức thanh toán nào chưa
	if (!chonhinhthuc) {
		text = "Chưa chọn hình thức thanh toán";
		document.getElementById(thongbao).innerHTML = text;
	}
	else {
		window.location = '/Checkout/AddBill?ngaydathang=' + ngaydh + '&phuongthucthanhtoan=' + pttt + '&diachigh=' + diachigh + '&TenNguoidung=' + tennd + '&SodienthoaiNv=' + sdt + '&tongtienhoadon=' + tongtien;
	}
}
function clear_Check(thongbao) {
	if (document.getElementById(thongbao).innerText == "valid") {
		document.getElementById(thongbao).innerHTML = "";
	}
}