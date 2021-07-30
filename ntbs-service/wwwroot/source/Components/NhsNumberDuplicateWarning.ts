import Vue from "vue";
import {getHeaders, buildPath} from "../helpers";
import axios, {Method} from "axios";

const NhsNumberDuplicateWarning = Vue.extend({
    methods: {
        handleValid: function (value: String) {
            const notificationId = (document.querySelector("#NotificationId") as HTMLInputElement).value;
            const requestConfig = {
                method: "post" as Method,
                url: buildPath("NhsNumberDuplicates"),
                headers: getHeaders(),
                data: {
                    "notificationId": notificationId,
                    "nhsNumber": value
                }
            };

            axios.request(requestConfig)
                .then((response: any) => {
                    if (response.data && Object.keys(response.data).length !== 0) {
                        this.displayWarnings(response.data);
                    } else {
                        this.hideWarnings();
                    }
                })
                .catch((error: any) => {
                    console.log(error.response);
                });
        },
        handleInvalid: function () {
            this.hideWarnings();
        },
        hideWarnings: function () {
            this.$refs["nhs-number-warning"].classList.add("hidden");
        },
        displayWarnings: function (linkData: Object) {
            const warningContainer = this.$refs["warning-container"];
            warningContainer.innerHTML = "";

            let requiresPrependedComma = false;
            for (const id in linkData) {
                if (linkData.hasOwnProperty(id)) {
                    const url = (linkData as any)[id];
                    if (requiresPrependedComma) {
                        // New lines required to match output from .cshtml
                        warningContainer.appendChild(document.createTextNode("\r\n,\r\n"));
                    }
                    if (url) {
                        warningContainer.appendChild(this.createAnchorTag(id, url));
                    }
                    else {
                        warningContainer.appendChild(this.createSpanTag(id));
                    }
                    requiresPrependedComma = true;
                }
            }
            this.$refs["nhs-number-warning"].classList.remove("hidden");
        },
        createAnchorTag: function (id: string, url: string) {
            const anchorTag = document.createElement("a");
            anchorTag.href = url;
            anchorTag.target = "_blank";
            anchorTag.innerHTML = `#${id}`;
            anchorTag.title = `Open notification overview for notification #${id}`;
            return anchorTag;
        },
        createSpanTag: function (id: string) {
            const legacyTag = document.createElement("span");
            legacyTag.innerHTML = id;
            return legacyTag;
        }
    }
});

export default NhsNumberDuplicateWarning;