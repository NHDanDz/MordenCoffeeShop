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
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '140px';
	}
	else {
		if (/[~`!#$%\@^&*+=\-\[\]\\';)(,/{}|\\":<>\?]/g.test(name.value) || /[0-9]/.test(name.value)) {
			text = "Tên không chứa số hoặc ký tự đặc biệt !";
			document.getElementById(thongbao).innerHTML = text;
			document.getElementById('divthongbao').hidden = false;
			document.getElementById('divthongbao').style.top = '140px';
		}
		else {
			document.getElementById('divthongbao').hidden = true;
			document.getElementById(ten).value = chuanhoa(name.value);
		}
	}
}
// -------------------------------------------------------- Kiểm tra username
function checkuser(ten, thongbao) {
	var name = document.getElementById(ten);
	let text = "";
	if (name.value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '0px';
	}
	else {
		if (/[~`!#$%\@^&*+=\-\[\]\\';)(,/{}|\\":<>\?]/g.test(name.value)) {
			text = "Username không chứa ký tự đặc biệt !";
			document.getElementById(thongbao).innerHTML = text;
			document.getElementById('divthongbao').hidden = false;
			document.getElementById('divthongbao').style.top = '0px';
		}
		else {
			document.getElementById('divthongbao').hidden = true;
		}

	}
}
// -------------------------------------------------------- Kiểm tra password
function checkpass(ten, thongbao) {
	var name = document.getElementById(ten);
	let text = "";
	if (name.value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '70px';
	}
	else {
		document.getElementById('divthongbao').hidden = true;
	}
}
// -------------------------------------------------------- Kiểm tra giới tính
function checkgioitinh(ten, thongbao) {
	var selectedGender = document.getElementById(ten).value;

	// Kiểm tra nếu giá trị là "Giới tính" (giá trị mặc định)
	if (selectedGender === "Giới tính") {
		text = "Bạn chưa chọn giới tính !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '200px';
	}
	else {
		document.getElementById('divthongbao').hidden = true;
	}
}
// -------------------------------------------------------- Kiểm tra dịa chỉ
function checkdiachi(ten, thongbao) {
	var name = document.getElementById(ten);
	let text = "";
	if (name.value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '340px';
	}
	else {
		document.getElementById('divthongbao').hidden = true;
		document.getElementById(ten).value = chuanhoa(name.value);
	}
}
// -------------------------------------------------------- Kiểm tra số điện thoại
function checksdt(ten, thongbao) {
	var sdt = document.getElementById(ten);
	var value = sdt.value;
	let text = "";
	// Kiểm tra rỗng
	if (value == "") {
		text = "Không để trống mục này !";
		document.getElementById(thongbao).innerHTML = text;
		document.getElementById('divthongbao').hidden = false;
		document.getElementById('divthongbao').style.top = '270px';
	}
	else {
		// Kiểm tra xem giá trị có phải là số hay không
		if (isNaN(value)) {
			text = "Không nhập ký tự vào mục này !";
			document.getElementById(thongbao).innerHTML = text;
			value = value.slice(0, -1);
			document.getElementById(ten).value = value;
			document.getElementById('divthongbao').hidden = false;
			document.getElementById('divthongbao').style.top = '270px';
		}
		else {
			if (value.length < sdt.maxLength) {
				text = "Số điện thoại cần đủ 10 chữ số !";
				document.getElementById(thongbao).innerHTML = text;
				document.getElementById('divthongbao').hidden = false;
				document.getElementById('divthongbao').style.top = '270px';
			}
			else {
				document.getElementById('divthongbao').hidden = true;
			}
		}
	}
}
// Kiểm tra trước khi đăng ký
function validateForm() {
	// Kiểm tra giá trị của các trường input
	checkdiachi('dc', 'thongbao');
	checksdt('sdt', 'thongbao');
	checkgioitinh('name', 'thongbao');
	checkten('tennd', 'thongbao');
	checkpass('pw', 'thongbao');
	checkuser('us', 'thongbao');
	document.getElementById('divthongbao').hidden = false;
	// Kiểm tra xem tất cả các giá trị có hợp lệ không
	var dc = document.getElementById('dc').value;
	var sdt = document.getElementById('sdt').value;
	var us = document.getElementById('us').value;
	var ten = document.getElementById('tennd').value;
	var pw = document.getElementById('pw').value;
	var gt = document.getElementById('name').value;
	if (dc == "" || sdt == "" || us == "" || ten == "" || pw == "" || gt === "Giới tính") {

	}
	else {
		document.getElementById("contactForm").submit();
	}
}