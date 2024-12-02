import axios from "axios";
import { useEffect } from "react";

export default function OptionsPlaces(props){

    // const {data}=props.data;
    const data={
        
            "origin": "SFO",
            "destination": "JFK",
            "date": "2024-06-20",
            "price": 2500
          
    }

    useEffect(()=>{
        const getOptions=async()=>{
            try{
                const response= await axios.get('https://localhost:7125/api/Travel/search-flights',{
                    params:{
                        origin: "SFO",
                        destination: "JFK",
                        date: "2024-06-25",
                        // "price": 2500
                    }
                })
                console.log('00000',response.data);

            }catch(error){
                console.log(error);
            }

        }
        getOptions();

    },[])

    return(<>
        </>)
}