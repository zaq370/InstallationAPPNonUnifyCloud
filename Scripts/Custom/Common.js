var Common = Common || {};
var ModelMessageObejct = ModelMessageObejct || {};
var Org = "";

(function () {
    $(document).ready(function () {
        Common.ReplaceErrorMessage();
        Common.AddStartToRequiredField();

        if (typeof organization !== 'undefined') {
            Org = organization;
        }

        $(document).on('invalid-form.validate', 'form', function () {
            var button = $(this).find(':submit');
            setTimeout(function () {
                button.removeAttr('disabled');
            }, 1);
        });
        $(document).on('submit', 'form', function () {
            var button = $(this).find(':submit');
            setTimeout(function () {
                button.attr('disabled', 'disabled');
            }, 0);
        });

    });

    function AddStartToRequiredField() {
        //如果Form有加入class: addRequiredStars，在這個form下面有設定Required的欄位自動在欄位名稱後面加上 *
        $("form.validationForm.addRequiredStars").each(function () {
            $(this).find("input,textarea").each(function () {
                if ($(this).attr("data-val-required")) {
                    $(this).parent().children(".fieldLabel").text($(this).parent().children(".fieldLabel").text() + " *");
                }
            });

        });
    }
    Common.AddStartToRequiredField = AddStartToRequiredField;

    function ReplaceErrorMessage() {
        //將model中annotation中的Resource.字串替換成該語系的訊息(前提是cshtml上有抓到 Model.Message)
        try {
            if ((modelMessage && modelMessage != null && modelMessage != "") || (fieldDisplayName && fieldDisplayName != null && fieldDisplayName != "")) {
                ModelMessageObejct = JSON.parse(modelMessage);

                //將ModelMessageObejct中有\'的字串替換掉
                for (var obj in ModelMessageObejct) {
                    ModelMessageObejct[obj] = ModelMessageObejct[obj].toString().replace("\\'", "'");
                }

                var formValidationMessage = jQuery(".validationForm").validate().settings.messages;
                var keys = Object.keys(formValidationMessage);
                for (var i = 0 ; i < keys.length ; i++) {
                    var validate = formValidationMessage[keys[i]];
                    var validateType = Object.keys(validate);
                    for (var j = 0 ; j < validateType.length ; j++) {
                        var message = validate[validateType[j]];
                        //if (message.toLocaleLowerCase().indexOf("resource.") == 0) {
                        //    var errorMessage = ModelMessageObejct[message.substr(9, message.length - 9)];
                        //    if (errorMessage) {
                        //        validate[validateType[j]] = errorMessage;
                        //    }
                        //} 
                        if (message.toLocaleLowerCase().indexOf("resource.") >= 0) {
                            var messageArray = message.split(" ");
                            var newMessage = "";
                            for (var k = 0 ; k < messageArray.length ; k++) {
                                if (messageArray[k].toLocaleLowerCase().indexOf("resource.") == 0) {
                                    var replaceMessage = ModelMessageObejct[messageArray[k].substr(9, messageArray[k].length - 9)];
                                    if (!replaceMessage) {
                                        replaceMessage = fieldDisplayName[messageArray[k]];
                                        if (!replaceMessage) {
                                            replaceMessage = messageArray[k];
                                        }
                                    }
                                    newMessage = newMessage + " " + replaceMessage;
                                } else {
                                    newMessage = newMessage + " " + messageArray[k];
                                }
                            }
                            validate[validateType[j]] = newMessage;
                        }
                    }
                }
            }
        } catch (e) {
        }
    }
    Common.ReplaceErrorMessage = ReplaceErrorMessage;

    // 轉義字元
    // 將畫面上使用者輸入的 < > & " '轉成字元實體 
    function EscapeHTML(a){
        a = "" + a;
        return a.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&apos;");
    }
    Common.EscapeHTML = EscapeHTML;

    // 將字元實體 轉成 < > & " '
    function UnescapeHTML(a) {
        a = "" + a;
        return a.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&amp;/g, "&").replace(/&quot;/g, '"').replace(/&apos;/g, "'");
    }
    Common.UnescapeHTML = UnescapeHTML;

    function AcceptChar(a) {

        var pattern = new RegExp("[`~#$^&=|{};\\[\\]~#￥……&（）——|{}【】‘；”“、？]", "gi");
        var result = a.replace(pattern,'');

        return result;
    }
    Common.AcceptChar = AcceptChar;

})();