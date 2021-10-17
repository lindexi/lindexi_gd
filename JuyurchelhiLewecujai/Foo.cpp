#include "Foo.h"

#include <iostream>
#include "vcclr.h"

void JuyurchelhiLewecujai::Foo::Output(System::String^ input)
{
	const pin_ptr<const wchar_t> p = PtrToStringChars(input);
	wchar_t const* c = p;
	wprintf(L"%s", c);

	//auto pinString = &input->GetPinnableReference();
	//wprintf(L"%s", pinString);
}
