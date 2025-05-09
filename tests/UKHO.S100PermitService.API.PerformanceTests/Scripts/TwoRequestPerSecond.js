import http from 'k6/http';
import { check } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
const Config = JSON.parse(open('./../config.json'));
const payload = JSON.parse(open('./../Data/Payload.json')); 

export const options = {
  scenarios: {
    TwoRequestPerSecond: {
      executor: 'constant-arrival-rate',
      rate: 2, // 2 iterations (requests) per second
      timeUnit: '1s', // per second
      duration: '10m', // run for 10 minute
      preAllocatedVUs: 5, // initial VUs to allocate
      maxVUs: 10, // maximum VUs to allow
    },
  },
};
export default function TwoRequestPerSecond() {
    
    let url = Config.Url;

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${Config.Token}`,
        },
    };  
    
    const res = http.post(url, JSON.stringify(payload), params);
    console.log(res.status);
    check(res, {
        'status is 200': (r) => r.status === 200,
    });
}
export function handleSummary(data) {
    return {
        ["./../Summary/TwoRequestPerSecond" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]: htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        ["./../Summary/TwoRequestPerSecond" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]: JSON.stringify(data),
    }
}